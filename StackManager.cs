namespace MyStackManager;

public class Solution {
  /*

  You are asked to design and implement a StackManager,
  which takes an INT array as an internal buffer (the array will be passed into StackManager during initialization, like ctor).
  The StackManager needs to provide following functionalities.

  User can request multiple stacks from the StackManager.
  Each stack could be used independently, with Push() and Pop().
  All stacks should use the internal buffer as the shared backend storage,
  which means all the elements got pushed into the stacks should be stored inside the shared buffer.
  After the stack is not used any more, user can return the stack back to StackManager to re-used space in internal buffer.
  Necessary vars can be used, but don't use another big array.

  Let n be the size of internal buffer
  Number of segments can reach n but rarely
  */

  public class StackManager {
    private const int INIT_STACK_SIZE = 10; // configurable for better performance
    private int[] _internalBuffer;
    private List<FreeSegment> _freeSegments;
    private Dictionary<int, Stack> _allocatedStacks;

    public StackManager(int[] internalBuffer) {
      _internalBuffer = internalBuffer;
      _freeSegments = new List<FreeSegment>() { new FreeSegment(0, internalBuffer.Length) };
      _allocatedStacks = new Dictionary<int, Stack>();
    }

    /// <summary>
    /// Request new instance of Stack
    /// TC: O(n)
    /// </summary>
    public Stack RequestNewStack(int size = INIT_STACK_SIZE) {
      for (var i = 0; i < _freeSegments.Count; i++) {
        if (_freeSegments[i].Size >= size) {
          int startIndexForStack = _freeSegments[i].StartIndex;
          _freeSegments[i].StartIndex += size;
          _freeSegments[i].Size -= size;
          if (_freeSegments[i].Size == 0) {
            _freeSegments.RemoveAt(i);
          }

          var stack = new Stack(_internalBuffer, startIndexForStack, size, this);
          _allocatedStacks.Add(startIndexForStack, stack);
          return stack;
        }
      }

      throw new OutOfMemoryException();
    }

    /// <summary>
    /// Return stack back to Stack manager when no longer used
    /// TC: O(n log n)
    /// </summary>
    public void ReturnStackBack(Stack stack) {
      if (!_allocatedStacks.ContainsKey(stack.StartIndex))
        throw new InvalidOperationException("Stack not found");

      _freeSegments.Add(new FreeSegment(stack.StartIndex, stack.Size));
      _allocatedStacks.Remove(stack.StartIndex);
      MergeFreeSegments();
    }

    /// <summary>
    /// Request extra memory for expanding a stack
    /// TC: O(n)
    /// </summary>
    internal void RequestExpansion(Stack stack, int extraSize) {
      int currentEnd = stack.StartIndex + stack.Size;

      // Try to expand contiguously first
      for (int i = 0; i < _freeSegments.Count; i++) {
        var freeStartIndex = _freeSegments[i].StartIndex;
        var freeSize = _freeSegments[i].Size;
        if (freeStartIndex == currentEnd && freeSize >= extraSize) {
          _freeSegments[i] = new FreeSegment(freeStartIndex + extraSize, freeSize - extraSize);
          if (_freeSegments[i].Size == 0)
            _freeSegments.RemoveAt(i);

          stack.Size += extraSize;
          return;
        }
      }

      // Contiguous expansion not possible, move to a new segment
      int newSize = stack.Size + extraSize;
      for (int i = 0; i < _freeSegments.Count; i++) {
        var freeStartIndex = _freeSegments[i].StartIndex;
        var freeSize = _freeSegments[i].Size;
        if (freeSize >= newSize) {
          _freeSegments[i] = new FreeSegment(freeStartIndex + newSize, freeSize - newSize);
          if (_freeSegments[i].Size == 0)
            _freeSegments.RemoveAt(i);

          Array.Copy(_internalBuffer, stack.StartIndex,
            _internalBuffer, freeStartIndex, stack.Size);

          ReturnStackBack(new Stack(_internalBuffer, stack.StartIndex, stack.Size, this));
          stack.StartIndex = freeStartIndex;
          stack.Size = newSize;
          return;
        }
      }

      // No segment found for expansion
      throw new OutOfMemoryException();
    }

    /// <summary>
    /// Merge free segments next to each other
    /// TC: O(n log n)
    /// </summary>
    private void MergeFreeSegments() {
      _freeSegments.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));
      for (int i = 0; i < _freeSegments.Count - 1; i++) {
        var current = _freeSegments[i];
        var next = _freeSegments[i + 1];
        if (current.StartIndex + current.StartIndex == next.StartIndex) {
          _freeSegments[i] = new FreeSegment(current.StartIndex, current.Size + next.Size);
          _freeSegments.RemoveAt(i + 1);
          i--;
        }
      }
    }
  }

  private class FreeSegment {
    public FreeSegment(int startIndex, int size) {
      StartIndex = startIndex;
      Size = size;
    }

    public int StartIndex { get; set; }
    public int Size { get; set; }
  }

  public class Stack {
    private int[] _internalBuffer;
    private int _size;
    private int _startIndex;
    private int _top;
    private StackManager _stackManager;

    internal Stack(int[] internalBuffer, int startIndex, int size, StackManager stackManager) {
      _internalBuffer = internalBuffer;
      _startIndex = startIndex;
      _size = size;
      _top = -1;
      _stackManager = stackManager;
    }

    /// <summary>
    /// Push element to the top of stack
    /// TC: Amortized to O(1)
    /// </summary>
    public void Push(int value) {
      if (_top + 1 >= _size) {
        _stackManager.RequestExpansion(this, _size);
      }

      _top++;
      _internalBuffer[_startIndex + _top] = value;
    }

    /// <summary>
    /// Pop the top of stack
    /// TC: Amortized to O(1)
    /// </summary>
    public int Pop() {
      if (_top < 0) {
        throw new InvalidOperationException("Stack has no element");
      }

      int value = _internalBuffer[_startIndex + _top];
      _top--;
      ReleaseUnusedMemory();
      return value;
    }

    public int StartIndex {
      get => _startIndex;
      internal set => _startIndex = value;
    }

    public int Size {
      get => _size;
      internal set => _size = value;
    }

    /// <summary>
    /// Released if the stack uses less than half of its allocated size
    /// TC: O(n)
    /// </summary>
    private void ReleaseUnusedMemory() {
      int usedSpace = _top + 1;
      int excessSpace = Size - usedSpace;

      if (excessSpace > Size / 2) {
        int shrinkBy = excessSpace / 2;
        _stackManager.ReturnStackBack(new Stack(_internalBuffer,
          _startIndex + Size - shrinkBy, shrinkBy, _stackManager));
        Size -= shrinkBy;
      }
    }
  }

  public static void Main() {
    StackManager manager = new StackManager(new int[50]);
    var stack = manager.RequestNewStack(5);

    // Push values to fill the stack
    stack.Push(1);
    stack.Push(2);
    stack.Push(3);
    stack.Push(4);
    stack.Push(5);

    var stack2 = manager.RequestNewStack(5);

    // Trigger expansion (move if necessary)
    stack.Push(6); // This will trigger the `RequestExpansion` method
    Console.WriteLine($"Stack Base Address: {stack.StartIndex}");
  }
}