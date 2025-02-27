using System;
using System.Collections.Generic;

public interface IDoableCommand<T>
    where T : class
{
    public IDoableCommand<T> Do(T target); //Returns the inverse of the command
}

public class IUndoStack<T>
    where T : class
{
    public IUndoStack(T target, Action<T>? onModify = null)
    {
        this.target = target;
        this.onModify = onModify == null ? (T Object) => { } : onModify;
    }


    public bool CanUndo() { return undoStack.Count > 0; }
    public bool CanRedo() { return redoStack.Count > 0; }

    public void AddCommand(IDoableCommand<T> command)
    {
        redoStack.Clear();
        undoStack.Push(command.Do(target));
        onModify(target);
    }

    public bool Undo()
    {
        if (CanUndo())
        {
            redoStack.Push(undoStack.Pop().Do(target));
            onModify(target);
            return true;
        }
        return false;
    }

    public bool Redo()
    {
        if (CanRedo())
        {
            undoStack.Push(redoStack.Pop().Do(target));
            onModify(target);
            return true;
        }
        return false;
    }

    private Stack<IDoableCommand<T>> undoStack = new Stack<IDoableCommand<T>>();
    private Stack<IDoableCommand<T>> redoStack = new Stack<IDoableCommand<T>>();
    private Action<T> onModify;

    private T target;
    public T Target
    {
        get { return target; }
        set { undoStack.Clear(); redoStack.Clear(); target = value; }
    }

}