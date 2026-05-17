$base = 'D:\杀戮尖塔2Mod\文档网页'
$targets = @{
    '手牌泛光' = 'hand_glow'
    '血条覆盖' = 'hp_bar'
    '修改手牌上限' = 'hand_limit'
    '数据保存' = 'data_save'
}

$output = @()

foreach ($keyword in $targets.Keys) {
    $key = $targets[$keyword]
    $filepath = Get-ChildItem "$base\*$keyword*.html" | Select-Object -First 1 -ExpandProperty FullName
    
    if (-not $filepath) {
        $output += "NOT FOUND: $keyword"
        continue
    }
    
    $content = Get-Content $filepath -Raw -Encoding UTF8
    $output += "===== $keyword ($key) ===== (size=$($content.Length))"
    
    # Find code blocks
    $matches = [regex]::Matches($content, '```[^\n]*\n(.*?)```', 'Singleline')
    $output += "Found $($matches.Count) code blocks"
    for ($i = 0; $i -lt $matches.Count; $i++) {
        $block = $matches[$i].Groups[1].Value
        $lines = @($block -split "`n" | Where-Object { $_.Trim() -ne '' })
        if ($lines.Count -gt 0) {
            $text = $lines[0..[Math]::Min(29, $lines.Count-1)] -join "`n"
            $output += "--- Code Block $($i+1) ---"
            $output += $text
        }
    }
    
    # Find article content
    $m = [regex]::Match($content, 'class="kira-post-content[^"]*"[^>]*>(.*?)(?:Author|Link)', 'Singleline')
    if ($m.Success) {
        $raw = $m.Groups[1].Value
        $text = $raw -replace '<script[^>]*>.*?</script>', '' -replace '<[^>]+>', "`n"
        $text = $text -replace '&lt;', '<' -replace '&gt;', '>' -replace '&amp;', '&' -replace '&nbsp;', ' '
        $text = $text -replace "`n\s*`n+", "`n`n"
        $lines = @($text -split "`n" | Where-Object { $_.Trim().Length -gt 10 })
        $output += "--- Article Text ---"
        $text = $lines[0..[Math]::Min(79, $lines.Count-1)] -join "`n"
        $output += $text
    }
    $output += ""
}

$output -join "`r`n" | Out-File 'D:\杀戮尖塔2Mod\PersonalMod\extracted_content.txt' -Encoding UTF8
Write-Host "Done"
