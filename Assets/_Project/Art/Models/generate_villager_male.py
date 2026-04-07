import bpy
import bmesh
import math

def create_male_villager():
    """Genera un popolano medievale maschile low-poly con rig base."""

    # --- 0. PULIZIA ---
    mat_names = ["Mat_Skin", "Mat_Tunic", "Mat_Pants", "Mat_Boots",
                 "Mat_Belt", "Mat_Hair", "Mat_Eye"]
    for name in mat_names:
        if name in bpy.data.materials:
            bpy.data.materials.remove(bpy.data.materials[name])

    if "Villager_Male" in bpy.data.objects:
        bpy.data.objects.remove(bpy.data.objects["Villager_Male"], do_unlink=True)

    def make_mat(name, color):
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes["Principled BSDF"]
        bsdf.inputs[0].default_value = color
        bsdf.inputs[2].default_value = 0.9  # Roughness alta per look opaco
        return mat

    # Palette medievale terrosa
    mat_skin  = make_mat("Mat_Skin",  (0.76, 0.57, 0.42, 1.0))
    mat_tunic = make_mat("Mat_Tunic", (0.28, 0.20, 0.12, 1.0))  # Marrone scuro
    mat_pants = make_mat("Mat_Pants", (0.22, 0.18, 0.14, 1.0))  # Grigio-marrone
    mat_boots = make_mat("Mat_Boots", (0.15, 0.10, 0.06, 1.0))  # Cuoio scuro
    mat_belt  = make_mat("Mat_Belt",  (0.12, 0.08, 0.04, 1.0))  # Cintura
    mat_hair  = make_mat("Mat_Hair",  (0.18, 0.12, 0.06, 1.0))  # Capelli castani
    mat_eye   = make_mat("Mat_Eye",   (0.1,  0.1,  0.1,  1.0))  # Occhi scuri

    mesh_parts = []

    # ======================================================
    # 1. GAMBE
    # ======================================================
    for side in [-1, 1]:
        x = 0.12 * side

        # Coscia (pantaloni)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0, 0.55))
        thigh = bpy.context.active_object
        thigh.scale = (0.13, 0.12, 0.28)
        bpy.ops.object.transform_apply(scale=True)
        thigh.data.materials.append(mat_pants)
        mesh_parts.append(thigh)

        # Stinco (pantaloni)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0, 0.25))
        shin = bpy.context.active_object
        shin.scale = (0.11, 0.10, 0.25)
        bpy.ops.object.transform_apply(scale=True)
        shin.data.materials.append(mat_pants)
        mesh_parts.append(shin)

        # Stivale
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x, 0.03, 0.06))
        boot = bpy.context.active_object
        boot.scale = (0.12, 0.16, 0.10)
        bpy.ops.object.transform_apply(scale=True)
        boot.data.materials.append(mat_boots)
        mesh_parts.append(boot)

    # ======================================================
    # 2. TORSO — Tunica
    # ======================================================
    # Busto principale
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 1.0))
    torso = bpy.context.active_object
    torso.scale = (0.22, 0.14, 0.30)
    bpy.ops.object.transform_apply(scale=True)
    torso.data.materials.append(mat_tunic)
    mesh_parts.append(torso)

    # Gonna della tunica (si allarga verso il basso)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.75))
    skirt = bpy.context.active_object
    skirt.scale = (0.25, 0.15, 0.12)
    bpy.ops.object.transform_apply(scale=True)
    skirt.data.materials.append(mat_tunic)
    mesh_parts.append(skirt)

    # Cintura
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.82))
    belt = bpy.context.active_object
    belt.scale = (0.24, 0.16, 0.03)
    bpy.ops.object.transform_apply(scale=True)
    belt.data.materials.append(mat_belt)
    mesh_parts.append(belt)

    # ======================================================
    # 3. BRACCIA
    # ======================================================
    for side in [-1, 1]:
        x_shoulder = 0.28 * side

        # Braccio superiore (tunica — manica)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x_shoulder, 0, 1.05))
        upper_arm = bpy.context.active_object
        upper_arm.scale = (0.08, 0.09, 0.18)
        bpy.ops.object.transform_apply(scale=True)
        upper_arm.data.materials.append(mat_tunic)
        mesh_parts.append(upper_arm)

        # Avambraccio (pelle esposta)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x_shoulder, 0, 0.85))
        forearm = bpy.context.active_object
        forearm.scale = (0.07, 0.07, 0.14)
        bpy.ops.object.transform_apply(scale=True)
        forearm.data.materials.append(mat_skin)
        mesh_parts.append(forearm)

        # Mano
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x_shoulder, 0, 0.72))
        hand = bpy.context.active_object
        hand.scale = (0.06, 0.04, 0.06)
        bpy.ops.object.transform_apply(scale=True)
        hand.data.materials.append(mat_skin)
        mesh_parts.append(hand)

    # ======================================================
    # 4. COLLO
    # ======================================================
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.06, depth=0.1,
                                         location=(0, 0, 1.22))
    neck = bpy.context.active_object
    neck.data.materials.append(mat_skin)
    mesh_parts.append(neck)

    # ======================================================
    # 5. TESTA
    # ======================================================
    # Cranio
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=6,
                                          radius=0.16, location=(0, 0, 1.40))
    head = bpy.context.active_object
    head.scale = (1.0, 0.9, 1.05)
    bpy.ops.object.transform_apply(scale=True)
    head.data.materials.append(mat_skin)
    mesh_parts.append(head)

    # Naso
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -0.15, 1.38))
    nose = bpy.context.active_object
    nose.scale = (0.025, 0.04, 0.035)
    bpy.ops.object.transform_apply(scale=True)
    nose.data.materials.append(mat_skin)
    mesh_parts.append(nose)

    # Occhi
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=6, ring_count=4,
                                              radius=0.02,
                                              location=(0.055 * side, -0.14, 1.42))
        eye = bpy.context.active_object
        eye.data.materials.append(mat_eye)
        mesh_parts.append(eye)

    # Orecchie
    for side in [-1, 1]:
        bpy.ops.mesh.primitive_cube_add(size=1,
                                         location=(0.16 * side, 0, 1.40))
        ear = bpy.context.active_object
        ear.scale = (0.02, 0.03, 0.04)
        bpy.ops.object.transform_apply(scale=True)
        ear.data.materials.append(mat_skin)
        mesh_parts.append(ear)

    # Capelli — calotta semplice
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4,
                                          radius=0.17, location=(0, 0.01, 1.43))
    hair = bpy.context.active_object
    hair.scale = (1.0, 0.95, 0.85)
    bpy.ops.object.transform_apply(scale=True)
    # Taglia la metà inferiore con bisect
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.bisect(plane_co=(0, 0, 1.40), plane_no=(0, 0, 1),
                         use_fill=True, clear_inner=True)
    bpy.ops.object.mode_set(mode='OBJECT')
    hair.data.materials.append(mat_hair)
    mesh_parts.append(hair)

    # ======================================================
    # 6. UNIONE MESH
    # ======================================================
    bpy.ops.object.select_all(action='DESELECT')
    for obj in mesh_parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = mesh_parts[0]
    bpy.ops.object.join()
    villager_mesh = bpy.context.active_object
    villager_mesh.name = "Villager_Male_Mesh"

    # Smooth shading parziale (angolo basso per mantenere look low-poly)
    bpy.ops.object.shade_smooth()
    villager_mesh.data.use_auto_smooth = True
    villager_mesh.data.auto_smooth_angle = math.radians(30)

    # ======================================================
    # 7. ARMATURE (RIG BASE)
    # ======================================================
    bpy.ops.object.armature_add(location=(0, 0, 0))
    armature_obj = bpy.context.active_object
    armature_obj.name = "Villager_Male_Rig"
    armature = armature_obj.data
    armature.name = "Villager_Male_Armature"

    bpy.ops.object.mode_set(mode='EDIT')

    # Root bone rinominata
    root_bone = armature.edit_bones["Bone"]
    root_bone.name = "Root"
    root_bone.head = (0, 0, 0)
    root_bone.tail = (0, 0, 0.15)

    def add_bone(name, head, tail, parent_name):
        bone = armature.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.parent = armature.edit_bones[parent_name]
        return bone

    # Spine
    add_bone("Spine",     (0, 0, 0.15),  (0, 0, 0.70),  "Root")
    add_bone("Chest",     (0, 0, 0.70),  (0, 0, 1.15),  "Spine")
    add_bone("Neck",      (0, 0, 1.15),  (0, 0, 1.27),  "Chest")
    add_bone("Head",      (0, 0, 1.27),  (0, 0, 1.55),  "Neck")

    # Gambe
    for side, sign in [("L", -1), ("R", 1)]:
        x = 0.12 * sign
        add_bone(f"UpperLeg.{side}", (x, 0, 0.70),  (x, 0, 0.40),  "Spine")
        add_bone(f"LowerLeg.{side}", (x, 0, 0.40),  (x, 0, 0.12),  f"UpperLeg.{side}")
        add_bone(f"Foot.{side}",     (x, 0, 0.12),  (x, 0.12, 0.02), f"LowerLeg.{side}")

    # Braccia
    for side, sign in [("L", -1), ("R", 1)]:
        x = 0.22 * sign
        x2 = 0.28 * sign
        add_bone(f"UpperArm.{side}", (x, 0, 1.10),  (x2, 0, 0.95), "Chest")
        add_bone(f"LowerArm.{side}", (x2, 0, 0.95), (x2, 0, 0.78), f"UpperArm.{side}")
        add_bone(f"Hand.{side}",     (x2, 0, 0.78), (x2, 0, 0.70), f"LowerArm.{side}")

    bpy.ops.object.mode_set(mode='OBJECT')

    # ======================================================
    # 8. PARENTING CON WEIGHT AUTOMATICI
    # ======================================================
    bpy.ops.object.select_all(action='DESELECT')
    villager_mesh.select_set(True)
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    # ======================================================
    # 9. ROOT EMPTY E ORGANIZZAZIONE
    # ======================================================
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, 0, 0))
    root = bpy.context.active_object
    root.name = "Villager_Male"
    armature_obj.parent = root

    # Pivot a piedi
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

    print("Successo: Popolano medievale maschile creato!")
    print(f"  Altezza ~1.55 unità Blender")
    print(f"  Bones: {len(armature.bones)}")
    print(f"  Mesh parts unite in: {villager_mesh.name}")
    return root


def export_fbx(filepath=None):
    """Esporta il villager come FBX per Unity."""
    import os
    if filepath is None:
        filepath = os.path.join(os.path.dirname(bpy.data.filepath),
                                "Characters", "villager_male.fbx")
    os.makedirs(os.path.dirname(filepath), exist_ok=True)

    bpy.ops.object.select_all(action='DESELECT')
    root = bpy.data.objects.get("Villager_Male")
    if root:
        root.select_set(True)
        for child in root.children_recursive:
            child.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        object_types={'EMPTY', 'ARMATURE', 'MESH'},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward='-Z',
        axis_up='Y',
    )
    print(f"Esportato FBX: {filepath}")


if __name__ == "__main__":
    create_male_villager()
    # Decommentare per esportare direttamente:
    # export_fbx()
