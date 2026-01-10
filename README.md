# descenders-chairlift-builder
**The most efficient way to build chairlifts in Unity.**

To use, download **ChairliftCableBuilder.cs** and put it into your assets folder in the Unity project. Then create a folder titled exactly "Editor" and drag the file into there.

<img width="398" height="680" alt="image" src="https://github.com/user-attachments/assets/47ecd36e-5ed1-4755-8871-482addf5238d" />

Create two line renderers, then drag them into the slots at the top for cable A and B. For wheel assemblies, those are the points the cables will connect to, so either select your wheel models in the prefab or create an invisible cube inside of the wheels. Add the wheel assemblies in order from your first tower to your last. For rollers I would keep the default settings. After selecting all wheel assemblies and putting them in order you can build cables, and then tweak the offset on the second cable to line it up however you want. For sag, I use 16 subdivisions and 1.5m, but you can do whatever your heart desires. For chairs, select a good prefab and drag it in. You do not need to edit the parents, as that is automatically done. Tweak the spacing, start offset, and chair offset to make it however you want. Finally, edit the last three options to make the chairs face correctly. You can build cables, generate the chairs only, or clear chairs with the last 3 options (building cables also automatically adds chairs.)

<img width="1497" height="971" alt="image" src="https://github.com/user-attachments/assets/caec6bcf-88d1-41db-b3d9-0ba75fd3fc01" />
