namespace AngleSharp.Dom.Events;

using System;
using Attributes;

[DomName("Event")]
public enum EventPhase : Byte
{
    [DomName("NONE")] None = 0,
    [DomName("CAPTURING_PHASE")] Capturing = 1,
    [DomName("AT_TARGET")] AtTarget = 2,
    [DomName("BUBBLING_PHASE")] Bubbling = 3
}