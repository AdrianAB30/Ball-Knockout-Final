using UnityEngine;
using TMPro;
using DG.Tweening;

public class TextJuicy : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private float jumpStrength = 10f; 
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float staggerTime = 0.1f; 
    [SerializeField] private Ease jumpEase = Ease.OutQuad;

    private float[] charOffsets;
    private TMP_MeshInfo[] cachedMeshInfo;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }
    void Start()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();

        textComponent.ForceMeshUpdate();

        int charCount = textComponent.textInfo.characterCount;
        charOffsets = new float[charCount];
        cachedMeshInfo = textComponent.textInfo.meshInfo;

        AnimateText();
    }

    void AnimateText()
    {
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < charOffsets.Length; i++)
        {
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int index = i;

            seq.Insert(i * staggerTime,
                DOTween.To(() => charOffsets[index], x => charOffsets[index] = x, jumpStrength, jumpDuration)
                .SetEase(jumpEase)
                .SetLoops(-1, LoopType.Yoyo) 
            );
        }
    }

    void Update()
    {
        ApplyOffsets();
    }

    void ApplyOffsets()
    {
        textComponent.ForceMeshUpdate();
        var textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

            Vector3 offset = new Vector3(0, charOffsets[i], 0);

            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

            destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] + offset;
            destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] + offset;
            destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] + offset;
            destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] + offset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}