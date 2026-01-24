namespace Macrosharp.UserInterfaces.ImageEditorWindow;

public sealed class ImageEditorState
{
    private ImageBuffer _raster;
    private ImageBuffer _matrix;
    private readonly ImageBuffer _original;
    private SyncFlag _syncFlag;
    private readonly List<ImageSnapshot> _undoStack = new();
    private readonly List<ImageSnapshot> _redoStack = new();
    private int _maxUndo = 20;

    public ImageEditorState(int width, int height)
    {
        _raster = new ImageBuffer(width, height);
        _matrix = new ImageBuffer(width, height);
        _raster.Fill(unchecked((int)0xFF1E1E1E));
        _matrix.Fill(unchecked((int)0xFF1E1E1E));
        _original = _raster.Clone();
        _syncFlag = SyncFlag.InSync;
    }

    public ImageBuffer GetRaster()
    {
        SyncToRaster();
        return _raster;
    }

    public ImageBuffer GetRasterCopy()
    {
        SyncToRaster();
        return _raster.Clone();
    }

    public ImageBuffer GetMatrix()
    {
        SyncToMatrix();
        return _matrix;
    }

    public int Width => _raster.Width;

    public int Height => _raster.Height;

    public void ApplyRasterEdit(Action<ImageBuffer> edit)
    {
        PushUndoSnapshot();
        edit(_raster);
        _syncFlag = SyncFlag.RasterNewer;
        SyncToMatrix();
    }

    public void ReplaceRaster(ImageBuffer newRaster)
    {
        PushUndoSnapshot();
        _raster = newRaster;
        _matrix = newRaster.Clone();
        _syncFlag = SyncFlag.InSync;
    }

    public void ApplyCrop(IntRect rect)
    {
        SyncToRaster();
        rect = rect.Normalize();
        rect = rect.Clamp(0, 0, _raster.Width, _raster.Height);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        PushUndoSnapshot();
        var cropped = _raster.Crop(rect);
        _raster = cropped;
        _matrix = cropped.Clone();
        _syncFlag = SyncFlag.InSync;
    }

    public bool TryUndo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        var snapshot = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _redoStack.Add(ImageSnapshot.From(_raster));
        Restore(snapshot);
        return true;
    }

    public bool TryRedo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        var snapshot = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        _undoStack.Add(ImageSnapshot.From(_raster));
        Restore(snapshot);
        return true;
    }

    public void ResetToOriginal()
    {
        PushUndoSnapshot();
        _raster = _original.Clone();
        _matrix = _original.Clone();
        _syncFlag = SyncFlag.InSync;
    }

    public void CommitMatrixToRaster()
    {
        if (_syncFlag == SyncFlag.MatrixNewer)
        {
            _raster.CopyFrom(_matrix);
            _syncFlag = SyncFlag.InSync;
        }
    }

    public void CommitRasterToMatrix()
    {
        if (_syncFlag == SyncFlag.RasterNewer)
        {
            _matrix.CopyFrom(_raster);
            _syncFlag = SyncFlag.InSync;
        }
    }

    public void MarkMatrixDirty()
    {
        _syncFlag = SyncFlag.MatrixNewer;
    }

    public void MarkRasterDirty()
    {
        _syncFlag = SyncFlag.RasterNewer;
    }

    private void PushUndoSnapshot()
    {
        _undoStack.Add(ImageSnapshot.From(_raster));
        if (_undoStack.Count > _maxUndo)
        {
            _undoStack.RemoveAt(0);
        }

        _redoStack.Clear();
    }

    private void Restore(ImageSnapshot snapshot)
    {
        _raster = snapshot.ToBuffer();
        _matrix = snapshot.ToBuffer();
        _syncFlag = SyncFlag.InSync;
    }

    private void SyncToRaster()
    {
        if (_syncFlag == SyncFlag.MatrixNewer)
        {
            _raster.CopyFrom(_matrix);
            _syncFlag = SyncFlag.InSync;
        }
    }

    private void SyncToMatrix()
    {
        if (_syncFlag == SyncFlag.RasterNewer)
        {
            _matrix.CopyFrom(_raster);
            _syncFlag = SyncFlag.InSync;
        }
    }
}

public enum SyncFlag
{
    InSync = 0,
    RasterNewer = 1,
    MatrixNewer = 2,
}
