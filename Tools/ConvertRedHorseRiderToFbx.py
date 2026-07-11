import bpy
import os
import sys


def output_path():
    arguments = sys.argv
    if "--" not in arguments:
        raise RuntimeError("Expected output FBX path after '--'.")
    return os.path.abspath(arguments[arguments.index("--") + 1])


target = output_path()
os.makedirs(os.path.dirname(target), exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(
    filepath=target,
    use_selection=False,
    apply_unit_scale=True,
    bake_anim=False,
    path_mode="AUTO",
    add_leaf_bones=False,
)
print("EXPORTED_FBX=" + target)
