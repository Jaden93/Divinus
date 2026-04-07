import bpy
import bmesh
import math
import random

def create_realistic_medieval_house_v9():
    # 1. Pulizia materiali
    for mat_name in ["Mat_Log", "Mat_Plank", "Mat_Roof_Plank", "Mat_Door"]:
        if mat_name in bpy.data.materials:
            bpy.data.materials.remove(bpy.data.materials[mat_name])

    def make_mat(name, color):
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs[0].default_value = color
        return mat

    mat_log = make_mat("Mat_Log", (0.2, 0.12, 0.06, 1.0))      # Tronchi grezzi
    mat_plank = make_mat("Mat_Plank", (0.35, 0.22, 0.12, 1.0))   # Pavimento
    mat_roof = make_mat("Mat_Roof_Plank", (0.25, 0.15, 0.08, 1.0)) # Assi tetto
    mat_door = make_mat("Mat_Door", (0.3, 0.18, 0.1, 1.0))      # Porta

    body_parts = []
    door_parts = []

    # --- 2. PAVIMENTO ---
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, 0.1))
    floor = bpy.context.active_object
    floor.scale = (3.9, 3.9, 0.2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    floor.data.materials.append(mat_plank)
    body_parts.append(floor)

    # --- 3. PARETI DI TRONCHI ---
    log_radius = 0.25
    house_w, house_d = 4, 4
    wall_h_logs = 10
    for h in range(wall_h_logs):
        z_pos = (log_radius * 2) + (h * log_radius * 1.7)
        for side in [-2, 2]: # Pareti X
            bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=log_radius, depth=house_d, location=(side, 0, z_pos))
            w_log = bpy.context.active_object
            w_log.rotation_euler[0] = math.radians(90)
            w_log.data.materials.append(mat_log)
            body_parts.append(w_log)
        for side in [-2, 2]: # Pareti Y
            if side == -2 and h < 7: # Porta
                for x_offset in [-1.6, 1.6]:
                    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=log_radius, depth=0.8, location=(x_offset, side, z_pos))
                    w_log = bpy.context.active_object
                    w_log.rotation_euler[1] = math.radians(90)
                    w_log.data.materials.append(mat_log)
                    body_parts.append(w_log)
            else:
                bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=log_radius, depth=house_w, location=(0, side, z_pos))
                w_log = bpy.context.active_object
                w_log.rotation_euler[1] = math.radians(90)
                w_log.data.materials.append(mat_log)
                body_parts.append(w_log)

    # --- 4. STRUTTURA TETTO (TRUSSES & PURLINS) ---
    roof_base_z = z_pos + 0.4
    # 3 Capriate (Trusses) - Davanti, Centro, Dietro
    for y_p in [-2, 0, 2]:
        for side_x in [-1, 1]:
            bpy.ops.mesh.primitive_cube_add(size=1, location=(1.1 * side_x, y_p, roof_base_z + 1.0))
            truss = bpy.context.active_object
            truss.scale = (2.8, 0.2, 0.2)
            truss.rotation_euler[1] = math.radians(45 * -side_x)
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            truss.data.materials.append(mat_log)
            body_parts.append(truss)

    # 5 Arcarecci (Purlins) - Travi orizzontali che collegano le capriate
    purlin_positions = [
        (0, roof_base_z + 2.0), # Colmo (Ridge)
        (1.5, roof_base_z + 0.6), (-1.5, roof_base_z + 0.6), # Bassi
        (0.8, roof_base_z + 1.3), (-0.8, roof_base_z + 1.3)  # Medi
    ]
    for px, pz in purlin_positions:
        bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.15, depth=5.0, location=(px, 0, pz))
        purlin = bpy.context.active_object
        purlin.rotation_euler[0] = math.radians(90)
        purlin.data.materials.append(mat_log)
        body_parts.append(purlin)

    # --- 5. COPERTURA A STRATI SOVRAPPOSTI (WOOD SHINGLES) ---
    # Creiamo 4 file di assi sovrapposte per ogni lato
    num_rows = 4
    plank_length = 1.2
    for side_x in [-1, 1]:
        for row in range(num_rows):
            # Calcolo posizione lungo la pendenza (45 gradi)
            offset = row * 0.7
            row_x = (0.5 + offset) * side_x
            row_z = (roof_base_z + 0.4) + offset
            
            # Creiamo lo strato di assi (un blocco unico con "scalino")
            bpy.ops.mesh.primitive_cube_add(size=1, location=(row_x, 0, row_z))
            plank_row = bpy.context.active_object
            plank_row.scale = (1.0, 5.4, 0.12)
            # Inclinazione leggermente diversa per farle "montare" una sull'altra
            plank_row.rotation_euler[1] = math.radians(48 * -side_x)
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            plank_row.data.materials.append(mat_roof)
            body_parts.append(plank_row)

    # Chiusura Colmo (Bargeboard/Cap)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, roof_base_z + 2.1))
    cap = bpy.context.active_object
    cap.scale = (0.4, 5.5, 0.2)
    cap.data.materials.append(mat_log)
    body_parts.append(cap)

    # UNIONE CORPO
    bpy.ops.object.select_all(action='DESELECT')
    for obj in body_parts: obj.select_set(True)
    bpy.context.view_layer.objects.active = body_parts[0]
    bpy.ops.object.join()
    house_body = bpy.context.active_object
    house_body.name = "House_Body"

    # --- 6. PORTA ---
    num_planks = 5
    plank_w, plank_h, plank_t = 0.24, 2.2, 0.1
    for i in range(num_planks):
        x_pos = -0.5 + (i * plank_w)
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x_pos, -2, 1.1))
        plank = bpy.context.active_object
        plank.scale = (plank_w * 0.95, plank_t, plank_h)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        plank.data.materials.append(mat_door)
        door_parts.append(plank)

    # Rinforzi Z porta
    for z_pos_brace, scale_x, rot_y in [(1.8, 1.2, 0), (0.4, 1.2, 0), (1.1, 1.4, 50)]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, -1.95, z_pos_brace))
        brace = bpy.context.active_object
        brace.scale = (scale_x, 0.12, 0.15)
        if rot_y != 0: brace.rotation_euler[1] = math.radians(rot_y)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        brace.data.materials.append(mat_log)
        door_parts.append(brace)

    # UNIONE PORTA
    bpy.ops.object.select_all(action='DESELECT')
    for obj in door_parts: obj.select_set(True)
    bpy.context.view_layer.objects.active = door_parts[0]
    bpy.ops.object.join()
    house_door = bpy.context.active_object
    house_door.name = "House_Door"

    # SPOSTAMENTO PIVOT PORTA
    bpy.context.scene.cursor.location = (-0.6, -2, 0.5)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

    # Parenting
    bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0,0,0))
    root = bpy.context.active_object
    root.name = "Medieval_Realistic_House"
    house_body.parent = root
    house_door.parent = root

    print("Successo: Casa medievale realistica creata!")

if __name__ == "__main__":
    create_realistic_medieval_house_v9()
