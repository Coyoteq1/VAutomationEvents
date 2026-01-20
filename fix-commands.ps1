# Remove problematic using alias from all command files
Get-ChildItem -Path "Commands" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match 'using Command = VampireCommandFramework\.CommandAttribute;') {
        $newContent = $content -replace 'using Command = VampireCommandFramework\.CommandAttribute;\r?\n', ''
        Set-Content -Path $_.FullName -Value $newContent -NoNewline
        Write-Host "Fixed: $($_.FullName)"
    }
}
Write-Host "Done!"
