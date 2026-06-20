import os
import re

dir_path = r'g:\UnityEditor\TimeAura\Assets\UI\Nexus'
# Fix project:/// to project://database/
pattern = re.compile(r"url\('project:///Assets/(.*?)'\)")

for filename in os.listdir(dir_path):
    if filename.endswith('.uss') or filename.endswith('.uxml'):
        file_path = os.path.join(dir_path, filename)
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        new_content = pattern.sub(r"url('project://database/Assets/\1')", content)
        
        if new_content != content:
            print(f"Fixed URLs in {filename}")
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
