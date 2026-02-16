DECLARE @ContractId INT = 4256; -- ✅ Remplace par ton contrat

SELECT 
    fs.Id AS SupportId,
    fs.ISIN,
    fs.Label,

    -- 💰 Montant net investi
    SUM(
        CASE 
            WHEN o.Type IN (1, 2, 3) THEN a.Amount
            WHEN o.Type IN (4, 5, 6) THEN -a.Amount
            ELSE 0
        END
    ) AS TotalNetInvested,

    -- 🔹 Parts détenues
    SUM(
        CASE 
            WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
            WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
            ELSE 0
        END
    ) AS TotalShares,

    -- 💸 PRU
    CASE 
        WHEN SUM(
            CASE 
                WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
                WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
                ELSE 0
            END
        ) = 0 THEN NULL
        ELSE 
            SUM(
                CASE 
                    WHEN o.Type IN (1, 2, 3) THEN a.Amount
                    WHEN o.Type IN (4, 5, 6) THEN -a.Amount
                    ELSE 0
                END
            ) /
            SUM(
                CASE 
                    WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
                    WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
                    ELSE 0
                END
            )
    END AS [PRU (€)],

    -- 📊 VL actuelle
    fs.LastValuationAmount AS [VL actuelle (€)],
    fs.LastValuationDate AS [Date VL],

    -- 💼 Valeur actuelle du support
    ROUND(
        SUM(
            CASE 
                WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
                WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
                ELSE 0
            END
        ) * fs.LastValuationAmount,
        2
    ) AS [Valeur actuelle (€)],

    -- 💹 Plus-value latente (€)
    ROUND(
        (
            SUM(
                CASE 
                    WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
                    WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
                    ELSE 0
                END
            ) * fs.LastValuationAmount
        ) -
        SUM(
            CASE 
                WHEN o.Type IN (1, 2, 3) THEN a.Amount
                WHEN o.Type IN (4, 5, 6) THEN -a.Amount
                ELSE 0
            END
        ),
        2
    ) AS [Plus-value (€)],

    -- 📈 Plus-value latente en %
    CASE 
        WHEN SUM(
            CASE 
                WHEN o.Type IN (1, 2, 3) THEN a.Amount
                WHEN o.Type IN (4, 5, 6) THEN -a.Amount
                ELSE 0
            END
        ) = 0 THEN NULL
        ELSE 
            ROUND(
                (
                    (
                        (
                            SUM(
                                CASE 
                                    WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
                                    WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
                                    ELSE 0
                                END
                            ) * fs.LastValuationAmount
                        ) -
                        SUM(
                            CASE 
                                WHEN o.Type IN (1, 2, 3) THEN a.Amount
                                WHEN o.Type IN (4, 5, 6) THEN -a.Amount
                                ELSE 0
                            END
                        )
                    ) /
                    SUM(
                        CASE 
                            WHEN o.Type IN (1, 2, 3) THEN a.Amount
                            WHEN o.Type IN (4, 5, 6) THEN -a.Amount
                            ELSE 0
                        END
                    )
                ) * 100,
                2
            )
    END AS [Plus-value (%)]

FROM dbo.OperationSupportAllocations a
INNER JOIN dbo.Operations o ON o.Id = a.OperationId
INNER JOIN dbo.FinancialSupports fs ON fs.Id = a.SupportId
WHERE o.ContractId = @ContractId
GROUP BY fs.Id, fs.ISIN, fs.Label, fs.LastValuationAmount, fs.LastValuationDate
HAVING 
    ABS(
        SUM(
            CASE 
                WHEN o.Type IN (1, 2, 3) THEN (a.Amount / NULLIF(a.NavAtOperation,0))
                WHEN o.Type IN (4, 5, 6) THEN -(a.Amount / NULLIF(a.NavAtOperation,0))
                ELSE 0
            END
        )
    ) > 0
ORDER BY fs.Label;
