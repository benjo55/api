-- 1. Création d'une fonction pour mettre un mot en nom propre
CREATE FUNCTION dbo.ProperCase (@input NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @output NVARCHAR(MAX) = ''
    DECLARE @word NVARCHAR(100)
    DECLARE @position INT = 1
    DECLARE @delimiter CHAR(1)
    DECLARE @i INT
    DECLARE @length INT

    -- Remplace les tirets et les espaces par un caractère spécial temporaire
    SET @input = LOWER(@input)
    SET @input = REPLACE(REPLACE(@input, '-', '|-|'), ' ', '| |')

    -- Boucle sur chaque segment
    WHILE CHARINDEX('|', @input) > 0
    BEGIN
        SET @word = LEFT(@input, CHARINDEX('|', @input) - 1)
        IF LEN(@word) > 0
            SET @word = UPPER(LEFT(@word, 1)) + SUBSTRING(@word, 2, LEN(@word))

        SET @output = @output + @word

        SET @delimiter = SUBSTRING(@input, CHARINDEX('|', @input) + 1, 1)
        SET @output = @output + 
            CASE @delimiter 
                WHEN '-' THEN '-'
                WHEN ' ' THEN ' '
                ELSE ''
            END

        SET @input = SUBSTRING(@input, CHARINDEX('|', @input) + 2, LEN(@input))
    END

    -- Dernier mot (s’il n'y a plus de séparateur)
    IF LEN(@input) > 0
        SET @output = @output + UPPER(LEFT(@input, 1)) + SUBSTRING(@input, 2, LEN(@input))

    RETURN @output
END
GO

-- 2. Exemple de mise à jour dans une table `Person`
UPDATE Person
SET
    FirstName = dbo.ProperCase(FirstName),
    LastName = dbo.ProperCase(LastName)
