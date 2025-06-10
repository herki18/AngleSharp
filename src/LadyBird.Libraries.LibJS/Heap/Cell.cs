namespace JS.Heap;

using System;

// Main implementation for Cell, converted from C++
public class Cell : GC.Cell
{
    // GC_CELL(Cell, GC::Cell); // C++ macro, not needed in C#

    // C++: virtual void initialize(Realm&);
    // C#: Virtual method, matches C++ signature
    public virtual void Initialize(Realm realm)
    {
        // Method body as in C++ (empty)
    }

    // C++: ALWAYS_INLINE VM& vm() const { return *reinterpret_cast<VM*>(private_data()); }
    // C#: Property, returns VM by casting PrivateData
    public VM Vm =>
        (VM)PrivateData;
}