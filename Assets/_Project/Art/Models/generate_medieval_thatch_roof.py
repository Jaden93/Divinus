import bpy
import bmesh
import math
import random

def create_standalone_thatch_roof():
    # 1. Pulizia materiali
    for mat_name in ["Mat_Roof_Wood", "Mat_Thatch_Straw"]:
        if mat_name in bpy.data.materials:
            bpy.data.materials.remove(bpy.data.materials[mat_name])

    def make_mat(name, color):
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs[0].default_value = color
        return mat

    mat_wood = make_mat("Mat_Roof_Wood", (0.2, 0.12, 0.06, 1.0))
    mat_thatch = make_mat("Mat_Thatch_Straw", (0.5, 0.38, 0.18, 1.0))

    roof_parts = []

    # --- PARAMETRI ---
    width = 4.8
    depth = 5.2
    height = 2.5
    thatch_thickness = 0.35

    # --- 2. STRUTTURA IN LEGNO (SOTTO IL TETTO) ---
    # Trave di Colmo (Ridge Beam)
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.15, depth=depth, location=(0, 0, height))
    ridge = bpy.context.active_object
    ridge.rotation_euler[0] = math.radians(90)
    ridge.data.materials.append(mat_wood)
    roof_parts.append(ridge)

    # Capriate (Rafters) - 3 coppie (fronte, centro, retro)
    for y_pos in [-depth/2 + 0.2, 0, depth/2 - 0.2]:
        for side in [-1, 1]:
            # Trave diagonale
            bpy.ops.mesh.primitive_cube_add(size=1, location=(width/4 * side, y_pos, height/2))
            rafter = bpy.context.active_object
            rafter.scale = (width/1.8, 0.15, 0.15)
            rafter.rotation_euler[1] = math.radians(45 * -side)
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            rafter.data.materials.append(mat_wood)
            roof_parts.append(rafter)

    # --- 3. COPERTURA IN PAGLIA (FASCI SOVRAPPOSTI) ---
    # Creiamo 3 livelli di paglia (basso, medio, alto) per lato
    levels = 3
    for side in [-1, 1]:
        for lvl in range(levels):
            lvl_height = (height / levels) * lvl + 0.4
            lvl_offset = (width / 2) - (lvl * 0.8)
            
            # Creiamo i singoli fasci (Bundles) per ogni livello
            num_bundles = 12
            for i in range(num_bundles):
                y_b = -depth/2 + (i * (depth / (num_bundles - 1)))
                
                # Ogni fascio è un cilindro schiacciato e irregolare
                bpy.ops.mesh.primitive_cylinder_add(vertices=5, radius=thatch_thickness/2, depth=1.5, location=(lvl_offset * side, y_b, lvl_height))
                bundle = bpy.context.active_object
                bundle.rotation_euler[1] = math.radians(48 * -side)
                bundle.scale = (1.0, 1.2, 0.6)
                
                # Irregolarità
                bundle.location.z += (random.random() - 0.5) * 0.1
                bundle.rotation_euler[0] += math.radians((random.random() - 0.5) * 5)
                
                bundle.data.materials.append(mat_thatch)
                roof_parts.append(bundle)

    # Chiusura Colmo (Fascio di paglia orizzontale in cima)
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.3, depth=depth + 0.2, location=(0, 0, height + 0.1))
    top_thatch = bpy.context.active_object
    top_thatch.rotation_euler[0] = math.radians(90)
    top_thatch.scale = (1.2, 1.0, 0.7)
    top_thatch.data.materials.append(mat_thatch)
    roof_parts.append(top_thatch)

    # --- 4. DETTAGLI ESTERNI (TRAVI DI RITENUTA) ---
    # Travi che schiacciano la paglia (tipico medievale)
    for y_p in [-1.5, 1.5]:
        for side in [-1, 1]:
            bpy.ops.mesh.primitive_cube_add(size=1, location=(width/3 * side, y_p, height/2 + 0.5))
            weight_beam = bpy.context.active_object
            weight_beam.scale = (0.1, 0.1, 2.8)
            weight_beam.rotation_euler[0] = math.radians(90)
            weight_beam.rotation_euler[1] = math.radians(45 * -side)
            weight_beam.data.materials.append(mat_wood)
            roof_parts.append(weight_beam)

    # --- 5. TIMPANI (GABLE ENDS) ---
    for y_g in [-depth/2 + 0.1, depth/2 - 0.1]:
        # Pannello di legno triangolare
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, y_g, height/2))
        gable = bpy.context.active_object
        gable.rotation_euler[0] = math.radians(90)
        gable.scale = (width/1.1, height, 1.0)
        
        # Taglio triangolare con BMesh
        bm = bmesh.new()
        bm.from_mesh(gable.data)
        for v in bm.verts:
            if v.co.z > 0: v.co.x = 0 # Punta il vertice superiore al centro
        bm.to_mesh(gable.data)
        bm.free()
        
        gable.data.materials.append(mat_wood)
        roof_parts.append(gable)

    # --- UNIONE FINALE ---
    bpy.ops.object.select_all(action='DESELECT')
    for obj in roof_parts: obj.select_set(True)
    bpy.context.view_layer.objects.active = roof_parts[0]
    bpy.ops.object.join()
    
    final_roof = bpy.context.active_object
    final_roof.name = "Medieval_Thatch_Roof"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    print("Mesh del tetto medievale creata con successo!")

if __name__ == "__main__":
    create_standalone_thatch_roof()
