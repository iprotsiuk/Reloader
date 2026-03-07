# STYLE Character Preview Scene Progress

## 2026-03-07

- Approved direction: create a project-owned preview scene under `_Project` rather than mutating the imported third-party demo scene.
- Initial target architecture: wrap preview characters in the existing NPC foundation shell so the lineup can evolve into real MainTown civilians later.
- Imported the STYLE character package into the Unity project under `Assets/STYLE - Character Customization Kit`.
- Verified the imported package contains usable rigged character FBXs such as `Man_FullRig.fbx`, `Man_Rig_Correct.fbx`, and `Woman_Rig_Correct.fbx`.
- Confirmed through the imported vendor demo scene that the STYLE characters are modular skinned-mesh hierarchies with enabled/disabled appearance parts, not single opaque meshes.
- Diagnosed the pink import issue as a render-pipeline/material mismatch: the project runs URP, while the imported STYLE pack contains many materials pointing at built-in or missing shader references.
- Extended the existing `Reloader/Weapons/Fix Lowpoly Materials For URP` editor fixer to include the STYLE kit root and added focused EditMode coverage for that contract.
- Local-only checkpoint: a copied preview scene was used successfully for inspection in the editor, but the raw imported STYLE pack is currently a large uncurated third-party dump (`~1.9 GB`, `3009` files) and has not been committed to the branch yet.
- Deferred follow-up: curate the minimum STYLE subset we actually want in-repo, then commit a project-owned preview scene and NPC-shell-wrapped variants on top of that curated asset set.
