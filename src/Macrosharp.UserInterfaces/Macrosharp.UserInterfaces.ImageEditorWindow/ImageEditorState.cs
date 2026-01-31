namespace Macrosharp.UserInterfaces.ImageEditorWindow;

/// <summary>
/// Manages the image buffer state including raster pixels, drawing matrix, undo/redo history, and original state.
///
/// The state maintains two buffers:
/// - _raster: The final pixel buffer that represents the committed image (for saves)
/// - _matrix: The working buffer for drawing operations (can be reverted without affecting raster)
///
/// This dual-buffer system allows real-time feedback while preserving undo ability.
/// </summary>
public sealed class ImageEditorState
{
    private ImageBuffer _raster; // Committed pixel buffer (for saves)
    private ImageBuffer _matrix; // Working buffer for drawing
    private readonly ImageBuffer _original; // Original state for reset operation
    private SyncFlag _syncFlag; // Tracks which buffer is newer

    // Undo/Redo stacks storing snapshots of the raster buffer
    private readonly List<ImageSnapshot> _undoStack = new();
    private readonly List<ImageSnapshot> _redoStack = new();
    private int _maxUndo = 20;

    private bool _hasLoadedImage;

    /// <summary>
    /// Initializes a new ImageEditorState with a blank canvas of the specified dimensions.
    /// </summary>
    public ImageEditorState(int width, int height)
    {
        _raster = new ImageBuffer(width, height);
        _matrix = new ImageBuffer(width, height);
        _raster.Fill(unchecked((int)0xFF1E1E1E));
        _matrix.Fill(unchecked((int)0xFF1E1E1E));
        _original = _raster.Clone();
        _syncFlag = SyncFlag.InSync;
        _hasLoadedImage = false;
    }

    public bool HasLoadedImage => _hasLoadedImage;

    /// <summary>
    /// Marks that the user has provided an image (to show toolbar/info only with images).
    /// </summary>
    public void MarkUserHasImage()
    {
        _hasLoadedImage = true;
    }

    /// <summary>
    /// Gets the raster buffer after syncing from matrix if needed.
    /// Use this for saving to file.
    /// </summary>
    public ImageBuffer GetRaster()
    {
        SyncToRaster();
        return _raster;
    }

    /// <summary>
    /// Returns a copy of the raster buffer.
    /// Useful for operations that need a snapshot without the risk of modification.
    /// </summary>
    public ImageBuffer GetRasterCopy()
    {
        SyncToRaster();
        return _raster.Clone();
    }

    /// <summary>
    /// Gets the matrix (working) buffer after syncing from raster if needed.
    /// Use this for drawing operations.
    /// </summary>
    public ImageBuffer GetMatrix()
    {
        SyncToMatrix();
        return _matrix;
    }

    public int Width => _raster.Width;
    public int Height => _raster.Height;

    /// <summary>
    /// Applies an edit operation to the raster buffer and records undo history.
    /// </summary>
    public void ApplyRasterEdit(Action<ImageBuffer> edit)
    {
        PushUndoSnapshot();
        edit(_raster);
        _syncFlag = SyncFlag.RasterNewer;
        SyncToMatrix();
    }

    /// <summary>
    /// Replaces the entire image with a new buffer (typically from file or clipboard).
    /// </summary>
    public void ReplaceRaster(ImageBuffer newRaster)
    {
        PushUndoSnapshot();
        _raster = newRaster;
        _matrix = newRaster.Clone();
        _syncFlag = SyncFlag.InSync;
        _hasLoadedImage = true;
    }

    /// <summary>
    /// Resizes a blank canvas to match viewport dimensions.
    /// Only works if no image has been loaded yet.
    /// </summary>
    public void ResizeBlankToViewport(int width, int height)
    {
        if (_hasLoadedImage)
        {
            return;
        }

        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (_raster.Width == width && _raster.Height == height)
        {
            return;
        }

        _raster = new ImageBuffer(width, height);
        _matrix = new ImageBuffer(width, height);
        _raster.Fill(unchecked((int)0xFF1E1E1E));
        _matrix.Fill(unchecked((int)0xFF1E1E1E));
        _syncFlag = SyncFlag.InSync;
    }

    /// <summary>
    /// Crops both raster and matrix to the specified rectangle.
    /// </summary>
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

    /// <summary>
    /// Performs an undo operation, restoring the previous raster state.
    /// </summary>
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

    /// <summary>
    /// Performs a redo operation, restoring the next raster state after an undo.
    /// </summary>
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

    /// <summary>
    /// Reverts to the original image that was loaded or created.
    /// </summary>
    public void ResetToOriginal()
    {
        PushUndoSnapshot();
        _raster = _original.Clone();
        _matrix = _original.Clone();
        _syncFlag = SyncFlag.InSync;
    }

    /// <summary>
    /// Commits drawing changes from the matrix back to the raster (for persistence).
    /// </summary>
    public void CommitMatrixToRaster()
    {
        if (_syncFlag == SyncFlag.MatrixNewer)
        {
            _raster.CopyFrom(_matrix);
            _syncFlag = SyncFlag.InSync;
        }
    }

    /// <summary>
    /// Updates the matrix from the raster (for display after raster edits).
    /// </summary>
    public void CommitRasterToMatrix()
    {
        if (_syncFlag == SyncFlag.RasterNewer)
        {
            _matrix.CopyFrom(_raster);
            _syncFlag = SyncFlag.InSync;
        }
    }

    /// <summary>
    /// Marks the matrix as having unsaved drawing changes.
    /// </summary>
    public void MarkMatrixDirty()
    {
        _syncFlag = SyncFlag.MatrixNewer;
    }

    /// <summary>
    /// Marks the raster as having uncommitted edits.
    /// </summary>
    public void MarkRasterDirty()
    {
        _syncFlag = SyncFlag.RasterNewer;
    }

    /// <summary>
    /// Records a snapshot of the current raster for undo history.
    /// Limits history to _maxUndo entries.
    /// </summary>
    public void PushUndoSnapshot()
    {
        _undoStack.Add(ImageSnapshot.From(_raster));
        if (_undoStack.Count > _maxUndo)
        {
            _undoStack.RemoveAt(0);
        }

        _redoStack.Clear();
    }

    /// <summary>
    /// Restores both raster and matrix to a saved snapshot state.
    /// </summary>
    private void Restore(ImageSnapshot snapshot)
    {
        _raster = snapshot.ToBuffer();
        _matrix = snapshot.ToBuffer();
        _syncFlag = SyncFlag.InSync;
    }

    /// <summary>
    /// Syncs matrix to raster if the raster is the newer buffer.
    /// </summary>
    private void SyncToRaster()
    {
        if (_syncFlag == SyncFlag.MatrixNewer)
        {
            _raster.CopyFrom(_matrix);
            _syncFlag = SyncFlag.InSync;
        }
    }

    /// <summary>
    /// Syncs raster to matrix if the matrix is the newer buffer.
    /// </summary>
    private void SyncToMatrix()
    {
        if (_syncFlag == SyncFlag.RasterNewer)
        {
            _matrix.CopyFrom(_raster);
            _syncFlag = SyncFlag.InSync;
        }
    }
}

/// <summary>
/// Tracks which buffer contains the most recent changes (for sync coordination).
/// </summary>
public enum SyncFlag
{
    InSync = 0,
    RasterNewer = 1,
    MatrixNewer = 2,
}
