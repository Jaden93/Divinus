import bpy
import bmesh
import math

def create_medieval_furniture():
    """Genera asset di arredamento medievale per il progetto Divinus."""

    # --- SETUP MATERIALI ---
    def make_mat(name, color, roughness=0.9, emission=False):
        if name in bpy.data.materials:
            bpy.data.materials.remove(bpy.data.materials[name])
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        bsdf = nodes["Principled BSDF"]
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        if emission:
            # Per versioni recenti di Blender (4.0+)
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = color
                bsdf.inputs["Emission Strength"].default_value = 2.0
            else:
                # Per versioni precedenti (3.x)
                bsdf.inputs["Emission"].default_value = color
        return mat

    mat_wood = make_mat("Mat_Wood_Dark", (0.15, 0.10, 0.06, 1.0))
    mat_straw = make_mat("Mat_Straw", (0.80, 0.70, 0.30, 1.0))
    mat_fabric = make_mat("Mat_Fabric_Rough", (0.75, 0.70, 0.65, 1.0))
    mat_iron = make_mat("Mat_Iron", (0.05, 0.05, 0.05, 1.0), roughness=0.4)
    mat_fire = make_mat("Mat_Flame", (1.0, 0.5, 0.1, 1.0), emission=True)

    # ======================================================
    # 1. LETTO (bed_medieval)
    # ======================================================
    def create_bed():
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.15))
        frame = bpy.context.active_object
        frame.name = "bed_medieval"
        frame.scale = (0.9, 1.9, 0.3)
        bpy.ops.object.transform_apply(scale=True)
        frame.data.materials.append(mat_wood)

        # Materasso
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.4))
        mattress = bpy.context.active_object
        mattress.name = "bed_mattress"
        mattress.scale = (0.8, 1.8, 0.25)
        bpy.ops.object.transform_apply(scale=True)
        mattress.data.materials.append(mat_straw)
        mattress.parent = frame

        # Cuscino
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0.7, 0.55))
        pillow = bpy.context.active_object
        pillow.name = "bed_pillow"
        pillow.scale = (0.6, 0.3, 0.1)
        bpy.ops.object.transform_apply(scale=True)
        pillow.data.materials.append(mat_fabric)
        pillow.parent = frame
        
        return frame

    # ======================================================
    # 2. SCRIVANIA (desk_medieval)
    # ======================================================
    def create_desk():
        # Piano superiore
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.8))
        desk = bpy.context.active_object
        desk.name = "desk_medieval"
        desk.scale = (1.2, 0.7, 0.05)
        bpy.ops.object.transform_apply(scale=True)
        desk.data.materials.append(mat_wood)

        # Gambe
        for x in [-0.5, 0.5]:
            for y in [-0.3, 0.3]:
                bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, 0.4))
                leg = bpy.context.active_object
                leg.scale = (0.05, 0.05, 0.4)
                bpy.ops.object.transform_apply(scale=True)
                leg.data.materials.append(mat_wood)
                leg.parent = desk
        
        return desk

    # ======================================================
    # 3. LUME / CANDELA (candle_medieval)
    # ======================================================
    def create_candle():
        # Base (piattino in ferro)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.15, depth=0.02, location=(0, 0, 0.01))
        base = bpy.context.active_object
        base.name = "candle_medieval"
        base.data.materials.append(mat_iron)

        # Candela (cera)
        bpy.ops.mesh.primitive_cylinder_add(radius=0.04, depth=0.15, location=(0, 0, 0.08))
        wax = bpy.context.active_object
        wax.name = "candle_wax"
        wax.data.materials.append(mat_fabric) 
        wax.parent = base

        # Fiamma (placeholder)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.02, location=(0, 0, 0.18))
        flame = bpy.context.active_object
        flame.name = "candle_flame"
        flame.scale = (0.6, 0.6, 1.2)
        bpy.ops.object.transform_apply(scale=True)
        flame.data.materials.append(mat_fire)
        flame.parent = base
        
        return base

    # ======================================================
    # 4. TORCIA DA MURO (wall_torch_medieval)
    # ======================================================
    def create_wall_torch():
        # Staffa da muro
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -0.05, 0))
        torch = bpy.context.active_object
        torch.name = "wall_torch_medieval"
        torch.scale = (0.05, 0.02, 0.2)
        bpy.ops.object.transform_apply(scale=True)
        torch.data.materials.append(mat_iron)

        # Braccio torcia
        bpy.ops.mesh.primitive_cylinder_add(radius=0.02, depth=0.3, location=(0, 0.08, 0.1))
        torch.rotation_euler[0] = math.radians(30)
        bpy.ops.object.transform_apply(rotation=True)
        handle = bpy.context.active_object
        handle.scale = (1, 1, 1)
        handle.data.materials.append(mat_wood)
        handle.parent = torch

        # Fiamma (placeholder)
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.04, location=(0, 0.15, 0.25))
        flame = bpy.context.active_object
        flame.name = "torch_flame"
        flame.scale = (0.8, 0.8, 1.5)
        bpy.ops.object.transform_apply(scale=True)
        flame.data.materials.append(mat_fire)
        flame.parent = torch
        
        return torch

    # Esecuzione
    create_bed().location = (0, 0, 0)
    create_desk().location = (3, 0, 0)
    create_candle().location = (0, 3, 0)
    create_wall_torch().location = (3, 3, 0)

# Avvia la creazione
if __name__ == "__main__":
    create_medieval_furniture()
