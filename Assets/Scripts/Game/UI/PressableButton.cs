using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Button with a squash animation driven by the "_Press" property of its shader.</summary>
public class PressableButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    private static readonly int PressId = Shader.PropertyToID("_Press");
    private static readonly int LockedId = Shader.PropertyToID("_Locked");

    public static Action ClickSound;

    public event Action Clicked;
    public event Action Rejected;

    /// <summary>Scale multiplier used by intro animations.</summary>
    public float Pop = 1f;

    public bool Interactable { get; private set; } = true;

    private Material _material;
    private float _press;
    private bool _held;
    private float _lastScale = -1f;

    public void Bind(RawImage image)
    {
        _material = image.material;
    }

    public void SetInteractable(bool value, bool lockedLook = true)
    {
        Interactable = value;
        if (_material != null && lockedLook)
            _material.SetFloat(LockedId, value ? 0f : 1f);
    }

    private void Update()
    {
        float target = _held ? 1f : 0f;
        float press = Mathf.MoveTowards(_press, target, Time.unscaledDeltaTime * 9f);
        float s = Pop * (1f - 0.06f * press);

        // Touching the transform dirties the canvas, so only do it when the value really changes.
        if (!Mathf.Approximately(press, _press) && _material != null)
            _material.SetFloat(PressId, press);
        _press = press;

        if (!Mathf.Approximately(s, _lastScale))
        {
            _lastScale = s;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    public void OnPointerDown(PointerEventData eventData) => _held = true;
    public void OnPointerUp(PointerEventData eventData) => _held = false;
    public void OnPointerExit(PointerEventData eventData) => _held = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!Interactable)
        {
            Rejected?.Invoke();
            return;
        }

        ClickSound?.Invoke();
        Clicked?.Invoke();
    }
}
