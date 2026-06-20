import os
import re

dir_path = r'g:\UnityEditor\TimeAura\Assets\UI\Nexus'
# Purge all url(...) and resource(...)
pattern_url = re.compile(r'url\(.*?\)')
pattern_res = re.compile(r'resource\(.*?\)')

for filename in os.listdir(dir_path):
    if filename.endswith('.uss') or filename.endswith('.uxml'):
        file_path = os.path.join(dir_path, filename)
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        new_content = pattern_url.sub("none", content)
        new_content = pattern_res.sub("none", new_content)
        
        if new_content != content:
            print(f"Purged all assets from {filename}")
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(new_content)
