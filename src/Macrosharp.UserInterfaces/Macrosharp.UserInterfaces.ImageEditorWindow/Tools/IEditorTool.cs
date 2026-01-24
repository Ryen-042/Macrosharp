using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public interface IEditorTool
{
    void OnMouseDown(ImageEditor editor, EditorInput input);
    void OnMouseMove(ImageEditor editor, EditorInput input);
    void OnMouseUp(ImageEditor editor, EditorInput input);
    void OnMouseWheel(ImageEditor editor, EditorInput input);
    void OnKeyDown(ImageEditor editor, VIRTUAL_KEY key, ModifierState modifiers);
    void OnCancel(ImageEditor editor);
    void OnRender(ImageEditor editor, HDC hdc, int width, int height);
}
