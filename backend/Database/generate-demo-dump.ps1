[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=localhost\SQLEXPRESS;Database=IncidentsDSI;Trusted_Connection=True;TrustServerCertificate=True;",
    [string]$OutputPath = (Join-Path $PSScriptRoot "IncidentsDSI-demo-data.sql")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

function ConvertTo-SqlLiteral {
    param(
        [AllowNull()][object]$Value,
        [Parameter(Mandatory)][string]$SqlType
    )

    if ($null -eq $Value -or $Value -is [System.DBNull]) {
        return "NULL"
    }

    if ($Value -is [byte[]]) {
        return "0x$([Convert]::ToHexString($Value))"
    }

    if ($SqlType -in @("nvarchar", "nchar", "ntext", "varchar", "char", "text")) {
        $prefix = if ($SqlType -in @("nvarchar", "nchar", "ntext")) { "N" } else { "" }
        $parts = [regex]::Split([string]$Value, "(\r\n|\n|\r)")
        $sqlParts = foreach ($part in $parts) {
            if ($part -eq "`r`n") {
                "CHAR(13) + CHAR(10)"
            }
            elseif ($part -eq "`n") {
                "CHAR(10)"
            }
            elseif ($part -eq "`r") {
                "CHAR(13)"
            }
            else {
                $escaped = $part.Replace("'", "''")
                "$prefix'$escaped'"
            }
        }
        return ($sqlParts -join " + ")
    }

    if ($Value -is [datetime]) {
        return "CONVERT(datetime2(7),N'$($Value.ToString('yyyy-MM-ddTHH:mm:ss.fffffff',[Globalization.CultureInfo]::InvariantCulture))',126)"
    }

    if ($Value -is [datetimeoffset]) {
        return "CONVERT(datetimeoffset(7),N'$($Value.ToString('yyyy-MM-ddTHH:mm:ss.fffffffzzz',[Globalization.CultureInfo]::InvariantCulture))',127)"
    }

    if ($Value -is [timespan]) {
        return "CONVERT(time,N'$($Value.ToString('c',[Globalization.CultureInfo]::InvariantCulture))')"
    }

    if ($Value -is [bool]) {
        return $(if ($Value) { "1" } else { "0" })
    }

    if ($Value -is [guid]) {
        return "CONVERT(uniqueidentifier,N'$Value')"
    }

    if ($Value -is [decimal] -or $Value -is [double] -or $Value -is [single]) {
        return $Value.ToString([Globalization.CultureInfo]::InvariantCulture)
    }

    return ([string]$Value).Replace("'", "''")
}

$tableNames = @(
    "Applications",
    "Criticites",
    "DomainesProjet",
    "Entites",
    "Responsables",
    "Risques",
    "Statuts",
    "StatutsEtape",
    "StatutsProjet",
    "TypesIncident",
    "IncidentNumberSequences",
    "ProjetNumberSequences",
    "Projets",
    "Incidents",
    "EtapesProjet",
    "ProjetResponsables"
)

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$connection.Open()
$writer = [System.IO.StreamWriter]::new($OutputPath, $false, [System.Text.UTF8Encoding]::new($false))

try {
    $writer.WriteLine("-- Donnees de demonstration IncidentsDSI exportees depuis SQL Server.")
    $writer.WriteLine("-- Genere le $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz').")
    $writer.WriteLine("-- Pre-requis : appliquer les migrations EF Core avant d'executer ce script.")
    $writer.WriteLine("-- Ce script remplace les donnees applicatives du schema dbo par ce jeu de demonstration.")
    $writer.WriteLine()
    $writer.WriteLine("USE [IncidentsDSI];")
    $writer.WriteLine("GO")
    $writer.WriteLine("SET NOCOUNT ON;")
    $writer.WriteLine("SET XACT_ABORT ON;")
    $writer.WriteLine("BEGIN TRANSACTION;")
    $writer.WriteLine()

    foreach ($tableName in @("ProjetResponsables", "EtapesProjet", "Incidents", "Projets", "IncidentNumberSequences", "ProjetNumberSequences", "TypesIncident", "StatutsProjet", "StatutsEtape", "Statuts", "Risques", "Responsables", "Entites", "DomainesProjet", "Criticites", "Applications")) {
        $writer.WriteLine("DELETE FROM [dbo].[$tableName];")
    }

    $writer.WriteLine()
    $writer.WriteLine("COMMIT TRANSACTION;")
    $writer.WriteLine("GO")
    $writer.WriteLine()

    foreach ($tableName in $tableNames) {
        $metadataCommand = $connection.CreateCommand()
        $metadataCommand.CommandText = @"
SELECT c.name, ty.name, c.is_identity
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = N'dbo' AND t.name = N'$tableName' AND c.is_computed = 0
ORDER BY c.column_id;
"@
        $metadataReader = $metadataCommand.ExecuteReader()
        $columns = [System.Collections.Generic.List[object]]::new()
        while ($metadataReader.Read()) {
            $columns.Add([pscustomobject]@{
                Name = [string]$metadataReader.GetValue(0)
                SqlType = [string]$metadataReader.GetValue(1)
                IsIdentity = [bool]$metadataReader.GetValue(2)
            })
        }
        $metadataReader.Close()
        $metadataCommand.Dispose()

        if ($columns.Count -eq 0) {
            throw "Table introuvable ou sans colonnes : $tableName"
        }

        $quotedColumns = ($columns | ForEach-Object { "[$($_.Name)]" }) -join ", "
        $identityColumn = $columns | Where-Object IsIdentity | Select-Object -First 1
        if ($null -ne $identityColumn) {
            $writer.WriteLine("SET IDENTITY_INSERT [dbo].[$tableName] ON;")
        }

        $dataCommand = $connection.CreateCommand()
        $dataCommand.CommandText = "SELECT $quotedColumns FROM [dbo].[$tableName];"
        $dataReader = $dataCommand.ExecuteReader()
        $rowCount = 0
        while ($dataReader.Read()) {
            $literals = for ($index = 0; $index -lt $columns.Count; $index++) {
                ConvertTo-SqlLiteral -Value $dataReader.GetValue($index) -SqlType $columns[$index].SqlType
            }
            $writer.WriteLine("INSERT INTO [dbo].[$tableName] ($quotedColumns) VALUES ($($literals -join ', '));")
            $rowCount++
        }
        $dataReader.Close()
        $dataCommand.Dispose()

        if ($null -ne $identityColumn) {
            $writer.WriteLine("SET IDENTITY_INSERT [dbo].[$tableName] OFF;")
        }

        $writer.WriteLine("-- $tableName : $rowCount ligne(s)")
        $writer.WriteLine("GO")
    }
}
finally {
    $writer.Dispose()
    $connection.Dispose()
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$normalized = [System.IO.File]::ReadAllText($OutputPath).TrimEnd("`r", "`n")
[System.IO.File]::WriteAllText($OutputPath, $normalized, $utf8)

Write-Host "Export termine : $OutputPath"
