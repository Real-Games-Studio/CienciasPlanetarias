 #if UNITY_EDITOR
 using UnityEditor;
 using UnityEngine;
 using TMPro;

 // Editor utility to apply a project's TMP font to all TMP_Text in the scene
 public static class FindForTMPTextEditor
 {
     [MenuItem("Tools/Apply Project Font to All TMP_Text")]
     public static void ApplyFontToAllTMPText()
     {
         // Find all components in the scene that hold the reference
    var refs = Object.FindObjectsByType<FindForTMPText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
         if (refs == null || refs.Length == 0)
         {
             EditorUtility.DisplayDialog("Apply Project Font", "No FindForTMPText component found in the scene. Add one and assign tmpRef.", "OK");
             return;
         }

         // Use the first non-null tmpRef found
         TMP_Text reference = null;
         foreach (var r in refs)
         {
             if (r != null && r.tmpRef != null)
             {
                 reference = r.tmpRef;
                 break;
             }
         }

         if (reference == null)
         {
             EditorUtility.DisplayDialog("Apply Project Font", "Found FindForTMPText but tmpRef is null. Assign a TMP_Text with the desired font.", "OK");
             return;
         }

         var fontAsset = reference.font;
         if (fontAsset == null)
         {
             EditorUtility.DisplayDialog("Apply Project Font", "The reference TMP_Text does not have a font assigned.", "OK");
             return;
         }

    var allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
         int count = 0;

         // Group undo so user can undo all changes at once
         int group = Undo.GetCurrentGroup();
         Undo.SetCurrentGroupName("Apply Project Font to All TMP_Text");

         foreach (var t in allTexts)
         {
             if (t == null) continue;
             if (t.font == fontAsset) continue; // already using the font
             Undo.RecordObject(t, "Apply Project Font");
             t.font = fontAsset;
             EditorUtility.SetDirty(t);
             count++;
         }

         Undo.CollapseUndoOperations(group);
         EditorUtility.DisplayDialog("Apply Project Font", $"Applied font to {count} TMP_Text objects.", "OK");
     }
 }

 #endif
