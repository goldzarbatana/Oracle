import re

bak_path = r'g:\UnityEditor\TimeAura\Assets\UI\Nexus\NexusScene.uxml.bak'
target_path = r'g:\UnityEditor\TimeAura\Assets\UI\Nexus\NexusScene.uxml'

with open(bak_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Replace the header with the stable one
header = '<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" xsi="http://www.w3.org/2001/XMLSchema-instance" engine="UnityEngine.UIElements" editor="UnityEngine.UIElements" noNamespaceSchemaLocation="../../../UIElementsSchema/UIElements.xsd" editor-extension-mode="False">'
content = re.sub(r'<ui:UXML.*?>', header, content, count=1)

# 2. Fix the Style tags
# Remove all style tags first
content = re.sub(r'<ui:Style.*?>', '', content)
content = re.sub(r'<Style.*?>', '', content)

# Add back the stable ones
styles = '\n    <Style src="project://database/Assets/UI/Nexus/NexusScene.uss?fileID=7433441132597879392&amp;guid=3ee0ef23c57fa2c4096a46ba201a93c1&amp;type=3#NexusScene"/>\n    <Style src="../Oracle/OracleWidget.uss" />'
content = content.replace(header, header + styles)

# 3. Purge the font poison from line 5 (or any style attribute)
content = re.sub(r'style="-unity-font-definition: resource\(.*?\);"', '', content)

# 4. Fix project:/// to project://database/
content = content.replace('project:///Assets/', 'project://database/Assets/')

with open(target_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Full restoration complete. Poison purged. Header stabilized.")
