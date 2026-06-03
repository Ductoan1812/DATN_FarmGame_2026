using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(EntityData))]
public class EntityDataEditor : Editor
{
    private double lastFrameTime;
    private int currentStageIndex = 0;
    private float frameDuration = 0.5f;
    private bool autoPlay = true;

    public override bool RequiresConstantRepaint()
    {
        return autoPlay;
    }

    public override void OnInspectorGUI()
    {
        EntityData data = (EntityData)target;

        StageModule stageModule = null;
        if (data.modules != null)
        {
            stageModule = data.modules.OfType<StageModule>().FirstOrDefault();
        }

        if (stageModule != null && stageModule.stages != null && stageModule.stages.Length > 0)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🌿 Crop Visual Preview", EditorStyles.boldLabel);
            
            if (autoPlay)
            {
                double time = EditorApplication.timeSinceStartup;
                if (time - lastFrameTime > frameDuration)
                {
                    currentStageIndex = (currentStageIndex + 1) % stageModule.stages.Length;
                    lastFrameTime = time;
                }
            }

            if (currentStageIndex >= stageModule.stages.Length)
                currentStageIndex = 0;

            Sprite currentSprite = stageModule.stages[currentStageIndex].sprite;
            
            if (currentSprite != null)
            {
                float height = 64f;
                float width = height * (currentSprite.rect.width / currentSprite.rect.height);
                
                Rect layoutRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, height);
                Rect drawRect = new Rect(layoutRect.x + (layoutRect.width - width) / 2f, layoutRect.y, width, height);
                
                DrawSprite(drawRect, currentSprite);
            }
            else
            {
                EditorGUILayout.HelpBox("Missing sprite at stage " + currentStageIndex, MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            autoPlay = EditorGUILayout.Toggle("Auto Play", autoPlay, GUILayout.Width(100));
            if (!autoPlay)
            {
                currentStageIndex = EditorGUILayout.IntSlider(currentStageIndex, 0, stageModule.stages.Length - 1);
            }
            else
            {
                EditorGUILayout.LabelField($"Stage: {currentStageIndex + 1} / {stageModule.stages.Length}");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // Vẽ các phần mặc định của ScriptableObject
        base.OnInspectorGUI();
    }

    private void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null) return;
        
        Rect spriteRect = sprite.rect;
        Texture2D tex = sprite.texture;
        
        if (tex != null)
        {
            Rect texCoords = new Rect(
                spriteRect.x / tex.width,
                spriteRect.y / tex.height,
                spriteRect.width / tex.width,
                spriteRect.height / tex.height
            );
            
            GUI.DrawTextureWithTexCoords(rect, tex, texCoords, true);
        }
    }
}
