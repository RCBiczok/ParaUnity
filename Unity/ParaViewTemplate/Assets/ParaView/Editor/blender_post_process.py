import sys
import bpy

argv = sys.argv
argv = argv[argv.index("--") + 1:]

for item in bpy.data.objects: 
	bpy.data.objects[item.name].select = True

bpy.ops.object.delete()

bpy.ops.import_scene.x3d(filepath=argv[0])

#for item in bpy.data.objects: 
#	if item.name == "Cube" or item.type == "LAMP" or item.type == "CAMERA":
#		bpy.data.objects[item.name].select = True
#	else:
#		bpy.data.objects[item.name].select = False
#
#bpy.ops.object.delete();

bpy.ops.wm.save_as_mainfile(filepath=argv[0].replace(".x3d",".blend"))

