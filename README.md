# descenders-chairlift-builder

**The most efficient way to build chairlifts in Unity.**

To get started, download **ChairliftCableBuilder.cs** and place it in your Unity project’s **Assets** folder.
Create a folder named exactly **Editor**, then move the script into it. Once imported, a new **Tools** tab will appear in the top menu bar.

---

<img width="398" height="680" alt="image" src="https://github.com/user-attachments/assets/47ecd36e-5ed1-4755-8871-482addf5238d" />

**Requirements**
- Unity LineRenderer components for cables
- Wheel assemblies must contain MeshRenderers (visible or invisible)

1. **Create the cables**

   * Create **two Line Renderers**.
   * Drag them into the slots at the top for **Cable A** and **Cable B**.

2. **Set up wheel assemblies**

   * Wheel assemblies define where the cables connect (rollers are detected by MeshRenderers under each assembly).
   * Either:

     * Select your wheel models directly in the prefab, **or**
     * Create an invisible cube inside each wheel.
   * Add the wheel assemblies **in order**, starting from the first tower and ending at the last.

3. **Roller settings**

   * By default, rollers are collected from MeshRenderers and sorted by local X, which works for most towers.

4. **Build and align cables**

   * Once all wheel assemblies are selected and ordered, **build the cables**.
   * Adjust the **offset on the second cable** to align it as desired.
   * For sag, a good starting point is:

     * **16 subdivisions**
     * **1.5 m sag**
        * Sag uses a simple parabolic approximation, not a physical catenary.
   * Feel free to adjust these values to your preference.

5. **Add chairs**

   * Choose a suitable chair prefab and drag it in.
   * You do **not** need to edit the parents, this is handled automatically.
   * Adjust:

     * Chair spacing
     * Start offset
     * Chair offset

6. **Finalize chair orientation**

   * Use the **last three options** to make the chairs face the correct direction.

7. **Utility options**

   * You can:

     * Build cables (this also generates chairs)
     * Generate chairs only
     * Clear chairs

---


<img width="1497" height="971" alt="image" src="https://github.com/user-attachments/assets/caec6bcf-88d1-41db-b3d9-0ba75fd3fc01" />
