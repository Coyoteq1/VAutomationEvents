import os
root = r"c:\dev\VAuto"
changed = []
for dirpath, dirs, files in os.walk(root):
    for f in files:
        if f.lower().endswith('.md'):
            path = os.path.join(dirpath, f)
            with open(path, 'r', encoding='utf-8', errors='replace') as fh:
                lines = fh.readlines()
            newlines = [l.rstrip('\n').rstrip() + '\n' for l in lines]
            if len(newlines)>0 and not newlines[-1].endswith('\n'):
                newlines[-1] = newlines[-1] + '\n'
            # ensure final newline
            if newlines and not newlines[-1].endswith('\n'):
                newlines[-1] = newlines[-1] + '\n'
            if newlines != lines:
                with open(path, 'w', encoding='utf-8') as fh:
                    fh.writelines(newlines)
                changed.append(path)
print('Modified %d files' % len(changed))
for c in changed:
    print(c)
