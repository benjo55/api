using api.Data;
using api.Dtos.TaxProfile;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace api.Services
{
    /// <summary>
    /// Moteur générique de calcul de la fiscalité applicable aux contrats d'assurance financière.
    /// Lit le profil fiscal en base et applique les règles paramétrées.
    /// </summary>
    public class TaxEngineService : ITaxEngineService
    {
        private readonly ApplicationDBContext _context;

        public TaxEngineService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<TaxSimulationResult> SimulateAsync(TaxSimulationRequest req)
        {
            var profile = await _context.TaxProfiles.FindAsync(req.TaxProfileId)
                ?? throw new KeyNotFoundException($"Profil fiscal {req.TaxProfileId} introuvable.");

            TaxRuleVersion? activeRuleVersion = null;
            try
            {
                activeRuleVersion = await _context.TaxRuleVersions
                    .Where(v => v.IsActive)
                    .OrderByDescending(v => v.EffectiveFrom)
                    .FirstOrDefaultAsync();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                // Tolérance runtime si les tables d'audit/versioning ne sont pas encore migrées.
                activeRuleVersion = null;
            }

            bool afterThreshold = req.ContractDurationYears >= profile.DurationThresholdYears;

            var result = new TaxSimulationResult
            {
                TaxProfileId = profile.Id,
                ProfileLabel = profile.Label,
                ContractFamily = profile.ContractFamily,
                IsAfterDurationThreshold = afterThreshold,
                AppliedTaxRuleVersionId = activeRuleVersion?.Id,
                EventType = req.EventType,
            };

            var notes = new List<string>();
            var warnings = new List<string>();

            if (req.IsNonResidentFiscal && req.Residency == TaxResidency.France)
            {
                warnings.Add("Incohérence détectée : IsNonResidentFiscal=true avec résidence France.");
            }

            if (req.EventType == FiscalEventType.Arbitrage)
            {
                notes.Add("Arbitrage interne : neutralité fiscale (pas d'IR ni de prélèvements sociaux immédiats). ");
                result.WithdrawalTax = new WithdrawalTaxDetail
                {
                    GrossGain = 0m,
                    GainAllowanceApplied = 0m,
                    NetTaxableGain = 0m,
                    IrRate = 0m,
                    IrAmount = 0m,
                    SocialChargesRate = 0m,
                    SocialChargesAmount = 0m,
                    TotalTax = 0m,
                    NetWithdrawal = 0m,
                    EffectiveTaxRate = 0m,
                    Breakdown = ["Arbitrage non fiscalisé"],
                };
            }
            else if (req.EventType == FiscalEventType.Advance || req.IsAdvance)
            {
                notes.Add("Avance : non considérée comme un rachat fiscal. Aucune quote-part de gain taxable.");
                result.WithdrawalTax = new WithdrawalTaxDetail
                {
                    GrossGain = 0m,
                    GainAllowanceApplied = 0m,
                    NetTaxableGain = 0m,
                    IrRate = 0m,
                    IrAmount = 0m,
                    SocialChargesRate = 0m,
                    SocialChargesAmount = 0m,
                    TotalTax = 0m,
                    NetWithdrawal = req.GrossWithdrawal,
                    EffectiveTaxRate = 0m,
                    Breakdown = ["Avance non fiscalisée"],
                };
            }
            else
            {
                // ─── Rachat en capital ────────────────────────────────────────────
                if (req.ExitMode == ExitMode.Capital || req.ExitMode == ExitMode.Both)
                {
                    if (IsPerFamily(profile.ContractFamily) && req.PerCompartments.Count > 0)
                    {
                        result.WithdrawalTax = ComputePerCompartmentWithdrawalTax(profile, req, notes, warnings);
                    }
                    else
                    {
                        result.WithdrawalTax = ComputeWithdrawalTax(profile, req, afterThreshold, notes, warnings);
                    }
                }
            }

            // ─── Sortie en rente ──────────────────────────────────────────────
            if (req.ExitMode == ExitMode.Rente || req.ExitMode == ExitMode.Both)
            {
                result.RenteTax = ComputeRenteTax(profile, req, notes);
            }

            // ─── Fiscalité décès ──────────────────────────────────────────────
            if (req.DeathCapital > 0 || req.ContributionsBefore70 > 0 || req.ContributionsAfter70 > 0)
            {
                result.DeathTax = ComputeDeathTax(profile, req, notes);
            }

            result.Notes = [.. notes];
            result.Warnings = [.. warnings];

            try
            {
                var persisted = await PersistComputationAsync(req, result, activeRuleVersion?.Id);
                result.TaxComputationId = persisted.Id;
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                warnings.Add("Historisation indisponible: tables d'audit fiscal non migrées.");
            }

            return result;
        }

        // ═════════════════════════════════════════════════════════════════════
        // RACHAT / CAPITAL
        // ═════════════════════════════════════════════════════════════════════

        private static WithdrawalTaxDetail ComputeWithdrawalTax(
            TaxProfile profile,
            TaxSimulationRequest req,
            bool afterThreshold,
            List<string> notes,
            List<string> warnings)
        {
            var breakdown = new List<string>();
            decimal gain = req.GainAmount;

            // 1. Abattement annuel (uniquement après seuil)
            decimal allowance = 0m;
            if (afterThreshold && (profile.GainAllowanceSingle.HasValue || profile.GainAllowanceCouple.HasValue))
            {
                allowance = req.IsCouple
                    ? (profile.GainAllowanceCouple ?? 0m)
                    : (profile.GainAllowanceSingle ?? 0m);

                if (allowance > 0)
                {
                    allowance = Math.Min(allowance, gain);
                    breakdown.Add($"Abattement annuel appliqué : {allowance:N2} €");
                }
            }

            decimal netGain = gain - allowance;

            // 2. Calcul IR
            decimal irAmount = 0m;
            decimal irRate = 0m;

            if (afterThreshold && profile.IrExemptAfterThreshold)
            {
                breakdown.Add("IR exonéré après le seuil de durée (ex : PEA).");
            }
            else if (req.ApplyProgressiveScale && profile.CanChooseBareme)
            {
                irRate = Math.Max(0m, req.ProgressiveScaleRate ?? profile.IrRateBeforeThreshold);
                irAmount = Round2(netGain * irRate / 100m);
                breakdown.Add($"IR barème progressif simulé ({irRate} %) : {irAmount:N2} €");
            }
            else if (!afterThreshold)
            {
                // Avant seuil → PFU ou barème
                irRate = profile.IrRateBeforeThreshold;
                irAmount = Round2(netGain * irRate / 100m);
                breakdown.Add($"IR avant seuil ({profile.DurationThresholdYears} ans) : {irRate} % → {irAmount:N2} €");
            }
            else
            {
                // Après seuil → taux réduit éventuel + plafond 150k€
                if (profile.ContributionCapForReducedRate.HasValue && profile.ContributionCapForReducedRate.Value > 0)
                {
                    decimal cap = profile.ContributionCapForReducedRate.Value;
                    decimal totalContrib = req.TotalContributionsAllContracts;

                    if (totalContrib <= cap)
                    {
                        // Tout au taux réduit
                        irRate = profile.IrRateAfterThreshold;
                        irAmount = Round2(netGain * irRate / 100m);
                        breakdown.Add($"IR taux réduit ({irRate} %, versements ≤ {cap:N0} €) : {irAmount:N2} €");
                    }
                    else
                    {
                        // Fraction au taux réduit, fraction au taux standard
                        // On calcule la proportion des gains soumis à chaque taux
                        decimal ratioReduced = Math.Max(0m, Math.Min(1m, cap / totalContrib));
                        decimal gainReduced = Round2(netGain * ratioReduced);
                        decimal gainStandard = netGain - gainReduced;

                        decimal irReduced = Round2(gainReduced * profile.IrRateAfterThreshold / 100m);
                        decimal irStandard = Round2(gainStandard * profile.IrRateAboveContributionCap / 100m);

                        irAmount = irReduced + irStandard;
                        irRate = netGain > 0 ? Round2(irAmount / netGain * 100m) : 0m;

                        breakdown.Add($"IR taux réduit ({profile.IrRateAfterThreshold} %) sur {gainReduced:N2} € : {irReduced:N2} €");
                        breakdown.Add($"IR taux standard ({profile.IrRateAboveContributionCap} %) sur {gainStandard:N2} € : {irStandard:N2} €");
                    }
                }
                else
                {
                    irRate = profile.IrRateAfterThreshold;
                    irAmount = Round2(netGain * irRate / 100m);
                    breakdown.Add($"IR après seuil ({irRate} %) : {irAmount:N2} €");
                }
            }

            // 3. Prélèvements sociaux
            decimal psAmount = 0m;
            if (req.SocialChargesExemptByTreaty || (req.IsNonResidentFiscal && req.Residency == TaxResidency.Eee))
            {
                breakdown.Add("Prélèvements sociaux exonérés selon la résidence/convention fiscale.");
            }
            else if (afterThreshold && profile.SocialChargesExemptAfterThreshold)
            {
                breakdown.Add("Prélèvements sociaux exonérés après le seuil.");
            }
            else
            {
                psAmount = Round2(netGain * profile.SocialChargesRate / 100m);
                breakdown.Add($"Prélèvements sociaux ({profile.SocialChargesRate} %) : {psAmount:N2} €");
            }

            if (req.AlreadyPaidSocialCharges > 0)
            {
                var before = psAmount;
                psAmount = Math.Max(0m, psAmount - req.AlreadyPaidSocialCharges);
                breakdown.Add($"PS déjà prélevés neutralisés : -{Math.Min(before, req.AlreadyPaidSocialCharges):N2} €");
            }

            if (req.ApplyProgressiveScale && !profile.CanChooseBareme)
            {
                warnings.Add("Option barème demandée mais non autorisée par ce profil : PFU conservé.");
            }

            decimal totalTax = irAmount + psAmount;
            decimal netWithdrawal = req.GrossWithdrawal - totalTax;
            decimal effectiveRate = req.GrossWithdrawal > 0 ? Round2(totalTax / req.GrossWithdrawal * 100m) : 0m;

            if (profile.CanChooseBareme)
                notes.Add("Option barème progressif de l'IR possible sur option du contribuable.");

            return new WithdrawalTaxDetail
            {
                GrossGain = gain,
                GainAllowanceApplied = allowance,
                NetTaxableGain = netGain,
                IrRate = irRate,
                IrAmount = irAmount,
                SocialChargesRate = profile.SocialChargesRate,
                SocialChargesAmount = psAmount,
                TotalTax = totalTax,
                NetWithdrawal = netWithdrawal,
                EffectiveTaxRate = effectiveRate,
                Breakdown = [.. breakdown],
            };
        }

        // ═════════════════════════════════════════════════════════════════════
        // RENTE
        // ═════════════════════════════════════════════════════════════════════

        private static RenteTaxDetail ComputeRenteTax(TaxProfile profile, TaxSimulationRequest req, List<string> notes)
        {
            decimal? partImposable = profile.RentePartImposable;
            if (!profile.RenteTaxedAsPension && req.AgeAtFirstAnnuityPayment.HasValue)
            {
                var age = req.AgeAtFirstAnnuityPayment.Value;
                partImposable = age switch
                {
                    < 50 => 70m,
                    >= 50 and <= 59 => 50m,
                    >= 60 and <= 69 => 40m,
                    _ => 30m,
                };
            }

            var detail = new RenteTaxDetail
            {
                TaxedAsPension = profile.RenteTaxedAsPension,
                PartImposable = partImposable,
            };

            var renteNotes = new List<string>();

            if (profile.RenteTaxedAsPension)
                renteNotes.Add("Rente imposée comme pension de retraite : barème progressif IR + prélèvements sociaux.");
            else if (profile.RentePartImposable.HasValue)
                renteNotes.Add($"Rente viagère à titre onéreux : fraction imposable de {profile.RentePartImposable} % (dépend de l'âge au 1er versement).");
            else
                renteNotes.Add("Rente : modalités d'imposition à déterminer selon le contrat.");

            detail.Notes = [.. renteNotes];
            return detail;
        }

        private static WithdrawalTaxDetail ComputePerCompartmentWithdrawalTax(
            TaxProfile profile,
            TaxSimulationRequest req,
            List<string> notes,
            List<string> warnings)
        {
            var details = new List<PerCompartmentTaxDetail>();
            var breakdown = new List<string>();

            decimal totalCapitalTaxable = 0m;
            decimal totalGainTaxable = 0m;
            decimal totalIr = 0m;
            decimal totalPs = 0m;

            var marginalRate = Math.Max(0m, req.ProgressiveScaleRate ?? 30m);
            if (req.ProgressiveScaleRate is null)
                warnings.Add("PER multi-compartiments: taux barème non fourni, simulation avec taux marginal 30 %.");

            foreach (var c in req.PerCompartments)
            {
                var notesComp = new List<string>();
                var capital = Math.Max(0m, c.CapitalAmount);
                var gains = Math.Max(0m, c.GainAmount);

                decimal capitalTaxable = 0m;
                decimal gainTaxable = gains;

                switch (c.Type)
                {
                    case PerCompartmentType.VoluntaryDeducted:
                        capitalTaxable = capital;
                        notesComp.Add("Capital imposable au barème IR (versements déduits à l'entrée).");
                        break;
                    case PerCompartmentType.VoluntaryNonDeducted:
                        capitalTaxable = 0m;
                        notesComp.Add("Capital exonéré IR (versements non déduits). Gains taxés PFU.");
                        break;
                    case PerCompartmentType.EmployeeSavings:
                        capitalTaxable = 0m;
                        notesComp.Add("Compartiment épargne salariale: capital exonéré, gains imposables.");
                        break;
                    case PerCompartmentType.Mandatory:
                        capitalTaxable = capital;
                        notesComp.Add("Compartiment obligatoire: sortie en rente généralement privilégiée.");
                        warnings.Add("PER compartiment obligatoire simulé en capital: vérifier l'éligibilité juridique.");
                        break;
                }

                var irCapital = Round2(capitalTaxable * marginalRate / 100m);
                var irGain = Round2(gainTaxable * profile.IrRateAboveContributionCap / 100m);
                var irAmount = irCapital + irGain;

                var psAmount = 0m;
                if (!(req.SocialChargesExemptByTreaty || (req.IsNonResidentFiscal && req.Residency == TaxResidency.Eee)))
                {
                    psAmount = Round2(gainTaxable * profile.SocialChargesRate / 100m);
                }

                var totalTax = irAmount + psAmount;

                details.Add(new PerCompartmentTaxDetail
                {
                    Type = c.Type,
                    CapitalTaxable = capitalTaxable,
                    GainTaxable = gainTaxable,
                    IrAmount = irAmount,
                    SocialChargesAmount = psAmount,
                    TotalTax = totalTax,
                    Notes = [.. notesComp],
                });

                totalCapitalTaxable += capitalTaxable;
                totalGainTaxable += gainTaxable;
                totalIr += irAmount;
                totalPs += psAmount;
            }

            if (req.AlreadyPaidSocialCharges > 0)
            {
                var before = totalPs;
                totalPs = Math.Max(0m, totalPs - req.AlreadyPaidSocialCharges);
                breakdown.Add($"PS déjà prélevés neutralisés : -{Math.Min(before, req.AlreadyPaidSocialCharges):N2} €");
            }

            var totalTaxAmount = totalIr + totalPs;
            var netWithdrawal = req.GrossWithdrawal - totalTaxAmount;
            var taxableBase = totalCapitalTaxable + totalGainTaxable;
            var effectiveRate = req.GrossWithdrawal > 0 ? Round2(totalTaxAmount / req.GrossWithdrawal * 100m) : 0m;
            var irRate = taxableBase > 0 ? Round2(totalIr / taxableBase * 100m) : 0m;

            breakdown.Add($"PER multi-compartiments: base capital taxable = {totalCapitalTaxable:N2} €");
            breakdown.Add($"PER multi-compartiments: base gains taxables = {totalGainTaxable:N2} €");

            notes.Add("Simulation PER multi-compartiments appliquée (volontaire, salariale, obligatoire). ");

            return new WithdrawalTaxDetail
            {
                GrossGain = totalGainTaxable,
                GainAllowanceApplied = 0m,
                NetTaxableGain = taxableBase,
                IrRate = irRate,
                IrAmount = totalIr,
                SocialChargesRate = profile.SocialChargesRate,
                SocialChargesAmount = totalPs,
                TotalTax = totalTaxAmount,
                NetWithdrawal = netWithdrawal,
                EffectiveTaxRate = effectiveRate,
                Breakdown = [.. breakdown],
                PerCompartmentTaxes = [.. details],
            };
        }

        // ═════════════════════════════════════════════════════════════════════
        // DÉCÈS
        // ═════════════════════════════════════════════════════════════════════

        private static DeathTaxDetail ComputeDeathTax(
            TaxProfile profile,
            TaxSimulationRequest req,
            List<string> notes)
        {
            var detail = new DeathTaxDetail();
            var deathNotes = new List<string>();

            if (req.Beneficiaries.Count > 0)
            {
                ComputeDeathTaxWithBeneficiaries(profile, req, detail, deathNotes);
                detail.Notes = [.. deathNotes];
                return detail;
            }

            // ─── Article 990 I – versements avant 70 ans ─────────────────────
            if (profile.HasDeathTaxArticle990I && req.ContributionsBefore70 > 0)
            {
                decimal totalAllowance = (profile.Death990I_AllowancePerBeneficiary ?? 0m) * req.BeneficiaryCount;
                decimal taxableBase = Math.Max(0m, req.ContributionsBefore70 - totalAllowance);

                decimal tax990I = 0m;
                if (taxableBase > 0 && profile.Death990I_Rate1.HasValue)
                {
                    decimal threshold = profile.Death990I_Rate1Threshold ?? 0m;
                    if (taxableBase <= threshold)
                    {
                        tax990I = Round2(taxableBase * profile.Death990I_Rate1.Value / 100m);
                    }
                    else
                    {
                        decimal tranche1 = Round2(threshold * profile.Death990I_Rate1.Value / 100m);
                        decimal tranche2 = Round2((taxableBase - threshold) * (profile.Death990I_Rate2 ?? profile.Death990I_Rate1.Value) / 100m);
                        tax990I = tranche1 + tranche2;
                    }
                }

                detail.Article990I_TaxableBase = req.ContributionsBefore70;
                detail.Article990I_AllowanceTotal = totalAllowance;
                detail.Article990I_TaxAmount = tax990I;

                deathNotes.Add($"Art. 990 I CGI : abattement {totalAllowance:N0} € ({req.BeneficiaryCount} bénéficiaire(s) × {profile.Death990I_AllowancePerBeneficiary:N0} €), taxe = {tax990I:N2} €.");
            }

            // ─── Article 757 B – versements après 70 ans ─────────────────────
            if (profile.HasDeathTaxArticle757B && req.ContributionsAfter70 > 0)
            {
                decimal globalAllowance = profile.Death757B_GlobalAllowance ?? 0m;
                decimal taxableContribs = Math.Max(0m, req.ContributionsAfter70 - globalAllowance);

                detail.Article757B_TaxableContributions = req.ContributionsAfter70;
                detail.Article757B_AllowanceApplied = Math.Min(globalAllowance, req.ContributionsAfter70);
                detail.Article757B_TaxableAfterAllowance = taxableContribs;

                deathNotes.Add($"Art. 757 B CGI : seuls les versements ({req.ContributionsAfter70:N2} €) sont taxés. Abattement global {globalAllowance:N0} €. Base taxable : {taxableContribs:N2} € (soumis aux droits de succession classiques). Les gains restent exonérés.");
            }

            if (!profile.HasDeathTaxArticle990I && !profile.HasDeathTaxArticle757B)
                deathNotes.Add("Ce type de contrat ne bénéficie pas d'avantage successoral spécifique.");

            detail.Notes = [.. deathNotes];
            return detail;
        }

        private static void ComputeDeathTaxWithBeneficiaries(
            TaxProfile profile,
            TaxSimulationRequest req,
            DeathTaxDetail detail,
            List<string> deathNotes)
        {
            var allocations = req.Beneficiaries
                .Where(b => b.SharePercent > 0)
                .ToList();

            if (allocations.Count == 0)
            {
                deathNotes.Add("Aucune ventilation bénéficiaire valide : bascule vers calcul agrégé.");
                return;
            }

            var usufructShare = ResolveUsufructShare(req);
            detail.AppliedUsufructSharePercent = usufructShare;

            var weighted = allocations.Select(b =>
            {
                var weight = Math.Max(0m, b.SharePercent);
                if (req.IsDismemberedClause)
                {
                    weight *= b.IsUsufructuary ? usufructShare : (100m - usufructShare);
                }
                return (benef: b, weight);
            }).ToList();

            var totalWeight = weighted.Sum(x => x.weight);
            if (totalWeight <= 0)
            {
                deathNotes.Add("Ventilation bénéficiaire invalide : poids total nul.");
                return;
            }

            var taxableAfter70Total = weighted
                .Where(x => !IsExemptBeneficiary(x.benef))
                .Sum(x => req.ContributionsAfter70 * (x.weight / totalWeight));

            var globalAllowance757B = profile.Death757B_GlobalAllowance ?? 0m;
            var usedAllowance757B = 0m;

            var perBeneficiary = new List<BeneficiaryTaxAllocationDetail>();
            decimal total990Tax = 0m;

            foreach (var item in weighted)
            {
                var ratio = item.weight / totalWeight;
                var b = item.benef;
                var exempt = IsExemptBeneficiary(b);
                var baseBefore70 = Round2(req.ContributionsBefore70 * ratio);
                var baseAfter70 = Round2(req.ContributionsAfter70 * ratio);

                decimal allowance990 = 0m;
                decimal taxable990 = 0m;
                decimal tax990 = 0m;

                if (!exempt && profile.HasDeathTaxArticle990I)
                {
                    allowance990 = profile.Death990I_AllowancePerBeneficiary ?? 0m;
                    taxable990 = Math.Max(0m, baseBefore70 - allowance990);
                    tax990 = ComputeTax990I(taxable990, profile);
                    total990Tax += tax990;
                }

                decimal allowance757Share = 0m;
                decimal taxable757 = 0m;
                if (!exempt && profile.HasDeathTaxArticle757B)
                {
                    if (taxableAfter70Total > 0)
                    {
                        var shareOfTaxable = baseAfter70 / taxableAfter70Total;
                        allowance757Share = Round2(globalAllowance757B * shareOfTaxable);
                    }

                    // Ajustement pour ne pas dépasser l'abattement global réparti
                    allowance757Share = Math.Min(allowance757Share, Math.Max(0m, globalAllowance757B - usedAllowance757B));
                    usedAllowance757B += allowance757Share;

                    taxable757 = Math.Max(0m, baseAfter70 - allowance757Share);
                }

                var label = string.IsNullOrWhiteSpace(b.Name)
                    ? $"Bénéficiaire {perBeneficiary.Count + 1}"
                    : b.Name!;

                perBeneficiary.Add(new BeneficiaryTaxAllocationDetail
                {
                    BeneficiaryLabel = label,
                    Relation = b.Relation,
                    IsExempt = exempt,
                    AllocationPercent = Round2(ratio * 100m),
                    BaseBefore70 = baseBefore70,
                    BaseAfter70 = baseAfter70,
                    Allowance990I = allowance990,
                    Taxable990I = taxable990,
                    Tax990I = tax990,
                    Allowance757BShare = allowance757Share,
                    Taxable757B = taxable757,
                    Notes = exempt
                        ? ["Exonération (conjoint/PACS ou exemption explicite)."]
                        : [],
                });
            }

            detail.BeneficiaryTaxAllocations = [.. perBeneficiary];
            detail.Article990I_TaxableBase = req.ContributionsBefore70;
            detail.Article990I_AllowanceTotal = perBeneficiary.Sum(x => x.Allowance990I);
            detail.Article990I_TaxAmount = total990Tax;
            detail.Article757B_TaxableContributions = req.ContributionsAfter70;
            detail.Article757B_AllowanceApplied = Math.Min(globalAllowance757B, usedAllowance757B);
            detail.Article757B_TaxableAfterAllowance = perBeneficiary.Sum(x => x.Taxable757B);

            deathNotes.Add("Simulation décès avancée: ventilation par bénéficiaire appliquée.");
            if (req.IsDismemberedClause)
            {
                deathNotes.Add($"Clause démembrée: clé économique simplifiée usufruit {usufructShare:N2}% / nue-propriété {100m - usufructShare:N2}%.");
            }
        }

        private static decimal ResolveUsufructShare(TaxSimulationRequest req)
        {
            if (!req.IsDismemberedClause) return 100m;
            if (req.UsufructSharePercent.HasValue)
                return Math.Max(0m, Math.Min(100m, req.UsufructSharePercent.Value));

            var age = req.UsufructuaryAgeAtDeath ?? 70;
            return age switch
            {
                <= 20 => 90m,
                <= 30 => 80m,
                <= 40 => 70m,
                <= 50 => 60m,
                <= 60 => 50m,
                <= 70 => 40m,
                <= 80 => 30m,
                <= 90 => 20m,
                _ => 10m,
            };
        }

        private static bool IsExemptBeneficiary(BeneficiaryAllocationInput b)
            => b.IsExempt || b.Relation is BeneficiaryRelation.Spouse or BeneficiaryRelation.PacsPartner;

        private static decimal ComputeTax990I(decimal taxableBase, TaxProfile profile)
        {
            if (taxableBase <= 0 || !profile.Death990I_Rate1.HasValue)
                return 0m;

            var threshold = profile.Death990I_Rate1Threshold ?? 0m;
            if (taxableBase <= threshold)
                return Round2(taxableBase * profile.Death990I_Rate1.Value / 100m);

            var tranche1 = Round2(threshold * profile.Death990I_Rate1.Value / 100m);
            var rate2 = profile.Death990I_Rate2 ?? profile.Death990I_Rate1.Value;
            var tranche2 = Round2((taxableBase - threshold) * rate2 / 100m);
            return tranche1 + tranche2;
        }

        private static bool IsPerFamily(ContractFamily family)
            => family is ContractFamily.PERIndividuel or ContractFamily.PERCollectif or ContractFamily.PERObligatoire;

        public async Task<List<TaxComputationDto>> GetRecentComputationsAsync(int take = 50)
        {
            var safeTake = Math.Clamp(take, 1, 200);
            try
            {
                return await _context.TaxComputations
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(safeTake)
                    .Select(x => new TaxComputationDto
                    {
                        Id = x.Id,
                        TaxProfileId = x.TaxProfileId,
                        TaxRuleVersionId = x.TaxRuleVersionId,
                        EventType = x.EventType,
                        GrossWithdrawal = x.GrossWithdrawal,
                        GainAmount = x.GainAmount,
                        TotalTax = x.TotalTax,
                        CreatedDate = x.CreatedDate,
                    })
                    .ToListAsync();
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                return [];
            }
        }

        private async Task<TaxComputation> PersistComputationAsync(
            TaxSimulationRequest request,
            TaxSimulationResult result,
            int? taxRuleVersionId)
        {
            var computation = new TaxComputation
            {
                TaxProfileId = request.TaxProfileId,
                TaxRuleVersionId = taxRuleVersionId,
                EventType = request.EventType,
                GrossWithdrawal = request.GrossWithdrawal,
                GainAmount = request.GainAmount,
                TotalTax = result.WithdrawalTax?.TotalTax,
                RequestJson = JsonSerializer.Serialize(request),
                ResultJson = JsonSerializer.Serialize(result),
                CreatedDate = DateTime.UtcNow,
            };

            _context.TaxComputations.Add(computation);
            await _context.SaveChangesAsync();

            _context.FiscalEvents.Add(new FiscalEvent
            {
                TaxComputationId = computation.Id,
                EventType = request.EventType,
                EventDate = DateTime.UtcNow,
                Label = $"Simulation {request.EventType}",
            });
            await _context.SaveChangesAsync();

            return computation;
        }

        private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
