import os
import re

dir_path = r'g:\UnityEditor\TimeAura\Assets\UI\Nexus'
pattern = re.compile(r'^\s*-unity-font-definition:\s*resource\(.*?\);', re.MULTILINE)

for filename in os.listdir(dir_path):
    if filename.endswith('.uss'):
        file_path = os.path.join(dir_path, filename)
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        new_content = pattern.sub('', content)
        
        if new_content != content:
            print(f"Cleaned {filename}")
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
