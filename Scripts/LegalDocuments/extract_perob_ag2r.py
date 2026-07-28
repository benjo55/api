from __future__ import annotations

from html import escape
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile


SOURCE = Path(
    sys.argv[1]
    if len(sys.argv) > 1
    else r"C:\Life\Documentation\PEROB\CG Perob\CG PEROB AG2R - 032022.pdf"
)
OUTPUT = Path(
    sys.argv[2]
    if len(sys.argv) > 2
    else r"C:\Life\api\Data\LegalDocuments\perob-ag2r-032022.json"
)
PDFTOTEXT = (
    shutil.which("pdftotext")
    or r"C:\Program Files\Git\mingw64\bin\pdftotext.exe"
)

ARTICLE_RE = re.compile(r"^Article\s+(\d+(?:\.\d+)+)\s*-?\s*(.*)$")
CHAPTER_RE = re.compile(r"^Titre\s+(\d+)\s*[-–]\s*(.*)$")
ANNEX_HEADING_RE = re.compile(r"^(\d+(?:\.\d+)*)(?:\.)?\s+(.+)$")
TOC_PAGE_RE = re.compile(r"\s+\d+\s*$")
PAGE_NUMBER_RE = re.compile(r"^\d+$")

LEXICON_TERMS = [
    "Affilié",
    "Age théorique",
    "Arbitrage",
    "Bénéficiaire",
    "Facteurs de durabilité",
    "Fonds de Retraite Professionnelle Supplémentaire",
    "Investissement durable",
    "Organismes de Placements Collectifs (OPC)",
    "Part patronale et part salariale",
    "Participation aux bénéfices",
    "Produit financier",
    "Rachat exceptionnel",
    "Réversataire",
    "Risque en matière de durabilité",
    "Table de mortalité",
    "Tarif de rente en vigueur",
    "Taux technique",
    "Unité de compte",
    "Versement issu de l’épargne salariale",
    "Versement obligatoire",
    "Versement volontaire déductible et non déductible",
]

BODY_OPENERS = (
    "A défaut",
    "Afin ",
    "Au ",
    "Aux ",
    "Ce ",
    "Ces ",
    "Cette ",
    "Dans ",
    "De plus",
    "En ",
    "Il ",
    "La ",
    "Le ",
    "Les ",
    "Lors",
    "Pour ",
    "Sauf ",
    "Seuls ",
    "Toute ",
    "Un ",
    "Une ",
)


def normalize_text(value: str) -> str:
    replacements = {
        "\u0002": "",
        "\u00ad": "",
        "\ufeff": "",
        "\u202f": " ",
        "\u2009": " ",
        "\u0007": "",
    }
    for source, target in replacements.items():
        value = value.replace(source, target)
    return re.sub(r"\s+", " ", value).strip()


def extract_raw_pages() -> list[list[str]]:
    if not Path(PDFTOTEXT).exists():
        raise FileNotFoundError("pdftotext est requis pour extraire ce document.")

    with tempfile.TemporaryDirectory() as directory:
        output = Path(directory) / "document.txt"
        subprocess.run(
            [
                PDFTOTEXT,
                "-raw",
                "-enc",
                "UTF-8",
                str(SOURCE),
                str(output),
            ],
            check=True,
        )
        text = output.read_text(encoding="utf-8")

    pages = []
    for page in text.split("\f"):
        lines = [normalize_text(line) for line in page.splitlines()]
        pages.append([line for line in lines if line])
    return pages


def clean_page_lines(lines: list[str], page_number: int) -> list[str]:
    cleaned = []
    for line in lines:
        if PAGE_NUMBER_RE.match(line):
            continue
        if line.startswith("Conditions Générales - Ambition Retraite Entreprise"):
            continue
        if page_number == 10 and line == "Conditions Générales":
            continue
        cleaned.append(line)
    return cleaned


def extract_toc_titles(pages: list[list[str]]):
    chapters: dict[str, str] = {}
    articles: dict[str, str] = {}
    current: list[str] = []

    def commit():
        if not current:
            return
        heading = normalize_text(" ".join(current))
        heading = TOC_PAGE_RE.sub("", heading)
        chapter = CHAPTER_RE.match(heading)
        article = ARTICLE_RE.match(heading)
        if chapter:
            chapters[chapter.group(1)] = normalize_text(chapter.group(2))
        elif article:
            articles[article.group(1)] = normalize_text(article.group(2))

    for page_number in range(2, 7):
        for line in clean_page_lines(pages[page_number - 1], page_number):
            if CHAPTER_RE.match(line) or ARTICLE_RE.match(line):
                commit()
                current = [line]
            elif current:
                current.append(line)

            if current and TOC_PAGE_RE.search(line):
                commit()
                current = []
    commit()
    return chapters, articles


def consume_known_heading(
    lines: list[str],
    index: int,
    prefix: str,
    expected_title: str | None,
):
    first = lines[index]
    if expected_title:
        combined = first
        index += 1
        matcher = CHAPTER_RE if first.startswith("Titre ") else ARTICLE_RE
        match = matcher.match(combined)
        current_title = normalize_text(match.group(2)).lstrip("- ").strip()
        while (
            index < len(lines)
            and current_title != expected_title
            and expected_title.startswith(current_title)
        ):
            combined = normalize_text(f"{combined} {lines[index]}")
            match = matcher.match(combined)
            current_title = normalize_text(match.group(2)).lstrip("- ").strip()
            index += 1
        return combined, index

    combined = first
    index += 1
    if index >= len(lines):
        return combined, index

    candidate = lines[index]
    if ARTICLE_RE.match(candidate) or CHAPTER_RE.match(candidate):
        return combined, index
    if candidate.startswith(BODY_OPENERS):
        return combined, index

    heading_text = ARTICLE_RE.sub(r"\2", combined)
    likely_wrapped = (
        candidate[:1].islower()
        or len(candidate) <= 34
        or heading_text.endswith(
            (
                " à",
                " au",
                " aux",
                " avec",
                " dans",
                " de",
                " des",
                " du",
                " en",
                " et",
                " la",
                " le",
                " les",
                " pour",
                " sur",
            )
        )
    )
    if likely_wrapped:
        combined = normalize_text(f"{combined} {candidate}")
        index += 1
    return combined, index


def lines_to_plain_text(lines: list[str]) -> str:
    result = ""
    for raw_line in lines:
        text = normalize_text(raw_line)
        if not text:
            continue
        if not result:
            result = text
        elif result.endswith("-") and text[:1].islower():
            result = result[:-1] + text
        elif text in {"–", "•"}:
            result += f" {text}"
        else:
            result += f" {text}"

    result = re.sub(r"\s+([,.;:!?])", r"\1", result)
    result = re.sub(r"([–•])\s+", r"\1 ", result)
    return normalize_text(result)


def plain_text_to_html(text: str) -> str:
    if not text:
        return ""
    bullet_parts = re.split(r"\s+[–•]\s*", text)
    if len(bullet_parts) == 1:
        return f"<p>{escape(text)}</p>"

    introduction = bullet_parts[0].strip()
    items = [part.strip() for part in bullet_parts[1:] if part.strip()]
    html = f"<p>{escape(introduction)}</p>" if introduction else ""
    if items:
        html += "<ul>" + "".join(
            f"<li>{escape(item)}</li>" for item in items
        ) + "</ul>"
    return html


def paragraph_node(lines: list[str]):
    plain_text = lines_to_plain_text(lines)
    if not plain_text:
        return None
    return {
        "type": "Paragraph",
        "title": "Paragraphe",
        "contentHtml": plain_text_to_html(plain_text),
        "plainText": plain_text,
        "includeInTableOfContents": False,
        "numberingStyle": "none",
        "children": [],
    }


def article_node(
    title: str,
    code: str | None,
    body: list[str],
    *,
    include_in_toc: bool,
):
    children = []
    paragraph = paragraph_node(body)
    if paragraph:
        children.append(paragraph)
    return {
        "type": "Article",
        "title": normalize_text(title),
        "businessCode": code,
        "includeInTableOfContents": include_in_toc,
        "numberingStyle": "manual" if code else "none",
        "children": children,
    }


def build_numbered_hierarchy(
    records: list[dict],
    include_in_toc: bool,
    *,
    preserve_codes: bool = True,
):
    roots: list[dict] = []
    nodes_by_code: dict[str, dict] = {}
    for record in records:
        node = article_node(
            record["title"],
            record["code"] if preserve_codes else None,
            record["body"],
            include_in_toc=include_in_toc,
        )
        if not preserve_codes:
            node["numberingStyle"] = None
        code = record["code"]
        nodes_by_code[code] = node
        parent_code = code.rsplit(".", 1)[0] if "." in code else None
        parent = nodes_by_code.get(parent_code) if parent_code else None
        if parent:
            parent["children"].append(node)
        else:
            roots.append(node)
    return roots


def extract_lexicon(pages: list[list[str]]):
    lines = clean_page_lines(pages[6], 7) + clean_page_lines(pages[7], 8)
    if lines and lines[0] == "Lexique":
        lines = lines[1:]

    terms = []
    current = None
    term_set = set(LEXICON_TERMS)
    for line in lines:
        if line in term_set:
            current = {"title": line, "body": []}
            terms.append(current)
        elif current:
            current["body"].append(line)

    return {
        "type": "Chapter",
        "title": "Lexique",
        "includeInTableOfContents": True,
        "startOnNewPage": True,
        "numberingStyle": "none",
        "children": [
            article_node(term["title"], None, term["body"], include_in_toc=False)
            for term in terms
        ],
    }


def extract_preamble(pages: list[list[str]]):
    lines = clean_page_lines(pages[8], 9)
    if lines and lines[0] == "Préambule":
        lines = lines[1:]
    return {
        "type": "Chapter",
        "title": "Préambule",
        "includeInTableOfContents": True,
        "startOnNewPage": True,
        "numberingStyle": "none",
        "children": [
            article_node(
                "Cadre contractuel",
                None,
                lines,
                include_in_toc=False,
            )
        ],
    }


def extract_general_terms(
    pages: list[list[str]],
    toc_chapters: dict[str, str],
    toc_articles: dict[str, str],
):
    lines = []
    for page_number in range(10, 27):
        lines.extend(clean_page_lines(pages[page_number - 1], page_number))

    chapters = []
    current_chapter = None
    current_article = None
    index = 0
    while index < len(lines):
        line = lines[index]
        chapter_match = CHAPTER_RE.match(line)
        article_match = ARTICLE_RE.match(line)

        if chapter_match:
            code = chapter_match.group(1)
            heading, index = consume_known_heading(
                lines,
                index,
                f"Titre {code} -",
                toc_chapters.get(code),
            )
            match = CHAPTER_RE.match(heading)
            current_chapter = {
                "code": code,
                "title": normalize_text(match.group(2)),
                "articles": [],
            }
            chapters.append(current_chapter)
            current_article = None
            continue

        if article_match:
            code = article_match.group(1)
            heading, index = consume_known_heading(
                lines,
                index,
                f"Article {code}",
                toc_articles.get(code),
            )
            match = ARTICLE_RE.match(heading)
            if current_chapter is None:
                raise ValueError(f"Article rencontré avant un titre: {heading}")
            current_article = {
                "code": code,
                "title": normalize_text(match.group(2)).lstrip("- ").strip(),
                "body": [],
            }
            current_chapter["articles"].append(current_article)
            continue

        if current_article:
            current_article["body"].append(line)
        index += 1

    return [
        {
            "type": "Chapter",
            "title": chapter["title"],
            "businessCode": chapter["code"],
            "includeInTableOfContents": True,
            "startOnNewPage": True,
            "numberingStyle": "manual",
            "children": build_numbered_hierarchy(
                chapter["articles"],
                include_in_toc=True,
            ),
        }
        for chapter in chapters
    ]


def looks_like_annex_heading(line: str):
    match = ANNEX_HEADING_RE.match(line)
    if not match:
        return None
    code, title = match.groups()
    if not title or not title[:1].isalpha() or title[:1].islower():
        return None
    if len(title) > 90:
        return None
    return code, title


def extract_annex(pages: list[list[str]]):
    lines = []
    for page_number in range(27, 30):
        lines.extend(clean_page_lines(pages[page_number - 1], page_number))

    introduction = []
    records = []
    current = None
    index = 0
    while index < len(lines):
        line = lines[index]
        heading = looks_like_annex_heading(line)
        if heading:
            code, title = heading
            combined = title
            index += 1
            while index < len(lines):
                candidate = lines[index]
                if (
                    looks_like_annex_heading(candidate)
                    or candidate.startswith(BODY_OPENERS)
                    or candidate in {"–", "•"}
                ):
                    break
                is_short_completion = (
                    combined.endswith(":")
                    and candidate[:1].isupper()
                    and len(candidate) <= 34
                )
                is_continuation = (
                    candidate[:1].islower()
                    or candidate.startswith("(")
                    or is_short_completion
                )
                if not is_continuation:
                    break
                combined = normalize_text(f"{combined} {candidate}")
                index += 1
                if is_short_completion:
                    break
            current = {"code": code, "title": combined, "body": []}
            records.append(current)
            continue

        if current:
            current["body"].append(line)
        elif not line.startswith("Annexe fiscale"):
            introduction.append(line)
        index += 1

    children = []
    introduction_node = article_node(
        "Cadre fiscal et social",
        None,
        introduction,
        include_in_toc=False,
    )
    if introduction_node["children"]:
        children.append(introduction_node)
    children.extend(
        build_numbered_hierarchy(
            records,
            include_in_toc=False,
            preserve_codes=False,
        )
    )

    return {
        "type": "Chapter",
        "title": "Annexe fiscale - Note relative au traitement fiscal et social des cotisations et des prestations",
        "includeInTableOfContents": True,
        "startOnNewPage": True,
        "numberingStyle": "none",
        "children": children,
    }


def count_nodes(nodes: list[dict]) -> int:
    return sum(1 + count_nodes(node.get("children", [])) for node in nodes)


def flatten(nodes: list[dict]):
    for node in nodes:
        yield node
        yield from flatten(node.get("children", []))


def validate_document(nodes: list[dict]):
    chapters = [
        node
        for node in nodes
        if node["type"] == "Chapter" and node.get("businessCode")
    ]
    chapter_codes = [node["businessCode"] for node in chapters]
    if chapter_codes != [str(number) for number in range(1, 9)]:
        raise ValueError(f"Chapitres juridiques inattendus: {chapter_codes}")

    article_codes = {
        node["businessCode"]
        for node in flatten(nodes)
        if node["type"] == "Article" and node.get("businessCode")
    }
    required = {"1.1.1", "3.6.2.1", "5.1.4", "6.2.5.7", "8.7"}
    missing = required - article_codes
    if missing:
        raise ValueError(f"Articles structurants manquants: {sorted(missing)}")

    corrupted = [
        node["title"]
        for node in flatten(nodes)
        if "�" in node.get("title", "") or "�" in node.get("plainText", "")
    ]
    if corrupted:
        raise ValueError(f"Caractères corrompus détectés: {corrupted[:3]}")


def main():
    pages = extract_raw_pages()
    toc_chapters, toc_articles = extract_toc_titles(pages)
    nodes = [
        extract_lexicon(pages),
        extract_preamble(pages),
        *extract_general_terms(pages, toc_chapters, toc_articles),
        extract_annex(pages),
    ]
    validate_document(nodes)

    document = {
        "code": "PEROB-AG2R-032022",
        "name": "Conditions Générales - Ambition Retraite Entreprise",
        "description": (
            "Plan d'épargne retraite obligatoire. "
            "Document source : CG PEROB AG2R - 03/2022."
        ),
        "type": "ProductGeneralTerms",
        "changeSummary": "Import structuré du document CG PEROB AG2R - 03/2022",
        "nodes": nodes,
    }

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(
        json.dumps(document, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    print(f"OUTPUT={OUTPUT}")
    print(f"CHAPTERS={len(nodes)}")
    print(f"NODES={count_nodes(nodes)}")
    for chapter in nodes:
        print(
            f"- {chapter.get('businessCode', '-')}: "
            f"{chapter['title']} ({count_nodes(chapter['children'])} descendants)"
        )


if __name__ == "__main__":
    main()
