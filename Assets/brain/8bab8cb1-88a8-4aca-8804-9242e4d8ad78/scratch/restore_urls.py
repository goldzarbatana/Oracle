import os
import re

dir_path = r'g:\UnityEditor\TimeAura\Assets\UI\Nexus'

# Restore URLs from backup logic
# Actually, I'll just look at the .bak file to see the originals if possible.
# But I can also just re-enable them if I know the pattern.

# Let's just fix the NexusScene.uss first by looking at what I purged.
# Wait, I'll just use a smarter approach. I'll read the backup of the USS if it exists.
