using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ArcInstance : MonoBehaviour
{
    // Backing fields
    [SerializeField] private Color _color = Color.white;
    [SerializeField, Range(0, 360)] private float _angle = 90f;
    [SerializeField, Range(0.01f, 1f)] private float _thickness = 0.1f;

    // Public properties that trigger shader update
    public Color color
    {
        get => _color;
        set
        {
            _color = value;
            UpdateProperties();
        }
    }

    public float angle
    {
        get => _angle;
        set
        {
            _angle = value;
            UpdateProperties();
        }
    }

    public float thickness
    {
        get => _thickness;
        set
        {
            _thickness = value;
            UpdateProperties();
        }
    }

    private MaterialPropertyBlock _propBlock;
    private Renderer _renderer;

    void Awake()
    {
        Init();
        UpdateProperties();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Init();
        UpdateProperties();
    }
#endif

    void Init()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();
    }

    public void UpdateProperties()
    {
        if (_renderer == null) return;
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_Color", _color);
        _propBlock.SetFloat("_Angle", _angle);
        _propBlock.SetFloat("_Thickness", _thickness);
        _renderer.SetPropertyBlock(_propBlock);
    }
}
