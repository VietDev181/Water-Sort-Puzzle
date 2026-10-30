using UnityEngine;

public abstract class ScreenBase : MonoBehaviour
{
    protected WaterSortApp App { get; private set; }

    public static T Create<T>(WaterSortApp app, RectTransform parent) where T : ScreenBase
    {
        var rt = UIKit.Rect(typeof(T).Name, parent, Vector2.zero, Vector2.zero);
        UIKit.Stretch(rt);
        var screen = rt.gameObject.AddComponent<T>();
        screen.App = app;
        return screen;
    }

    protected RectTransform Rt => (RectTransform)transform;
}
