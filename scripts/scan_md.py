import os, re, sys
root = r"c:\dev\VAuto"
md_files = []
for dirpath, dirs, files in os.walk(root):
    for f in files:
        if f.lower().endswith('.md'):
            md_files.append(os.path.join(dirpath, f))
md_files_sorted = sorted(md_files)

issues = {}
for path in md_files_sorted:
    try:
        with open(path, 'r', encoding='utf-8', errors='replace') as fh:
            text = fh.read()
    except Exception as e:
        print('ERR reading', path, e)
        continue
    lines = text.splitlines()
    long_lines = [i+1 for i,l in enumerate(lines) if len(l)>120]
    code_fence_no_lang = []
    fence_re = re.compile(r"^```(.*)$")
    in_fence = False
    for i,l in enumerate(lines):
        m = fence_re.match(l)
        if m:
            lang = m.group(1).strip()
            if not in_fence and lang=="":
                code_fence_no_lang.append(i+1)
            in_fence = not in_fence
    h1 = len(re.findall(r"^# [^#]", text, re.MULTILINE))
    has_final_newline = text.endswith('\n')
    trailing_ws = [i+1 for i,l in enumerate(lines) if l.rstrip()!=l]
    issues[path] = {
        'lines': len(lines),
        'long_lines': long_lines[:5],
        'long_lines_count': len(long_lines),
        'code_fence_no_lang_count': len(code_fence_no_lang),
        'code_fence_no_lang_lines': code_fence_no_lang[:5],
        'h1_count': h1,
        'trailing_ws_count': len(trailing_ws),
        'trailing_ws_lines': trailing_ws[:5],
        'has_final_newline': has_final_newline
    }

# Print summary for files with any issue
for p,info in issues.items():
    if info['long_lines_count']>0 or info['code_fence_no_lang_count']>0 or info['trailing_ws_count']>0 or info['h1_count']>1 or not info['has_final_newline']:
        print(p)
        print('  lines:', info['lines'])
        print('  long_lines_count:', info['long_lines_count'], 'sample:', info['long_lines'])
        print('  code_fence_no_lang_count:', info['code_fence_no_lang_count'], 'sample:', info['code_fence_no_lang_lines'])
        print('  h1_count:', info['h1_count'])
        print('  trailing_ws_count:', info['trailing_ws_count'], 'sample:', info['trailing_ws_lines'])
        print('  has_final_newline:', info['has_final_newline'])
        print()

print('\nScanned %d markdown files' % len(md_files_sorted))
