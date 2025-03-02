namespace AngleSharp.Dom.Events;

using Attributes;

[DomName("GlobalEventHandlers")]
[DomNoInterfaceObject]
public interface IGlobalEventHandlers
{
    [DomName("onabort")] event DomEventHandler Aborted;

    [DomName("onblur")] event DomEventHandler Blurred;

    [DomName("oncancel")] event DomEventHandler Cancelled;

    [DomName("oncanplay")] event DomEventHandler CanPlay;

    [DomName("oncanplaythrough")] event DomEventHandler CanPlayThrough;

    [DomName("onchange")] event DomEventHandler Changed;

    [DomName("onclick")] event DomEventHandler Clicked;

    [DomName("oncuechange")] event DomEventHandler CueChanged;

    [DomName("ondblclick")] event DomEventHandler DoubleClick;

    [DomName("ondrag")] event DomEventHandler Drag;

    [DomName("ondragend")] event DomEventHandler DragEnd;

    [DomName("ondragenter")] event DomEventHandler DragEnter;

    [DomName("ondragexit")] event DomEventHandler DragExit;

    [DomName("ondragleave")] event DomEventHandler DragLeave;

    [DomName("ondragover")] event DomEventHandler DragOver;

    [DomName("ondragstart")] event DomEventHandler DragStart;

    [DomName("ondrop")] event DomEventHandler Dropped;

    [DomName("ondurationchange")] event DomEventHandler DurationChanged;

    [DomName("onemptied")] event DomEventHandler Emptied;

    [DomName("onended")] event DomEventHandler Ended;

    [DomName("onerror")] event DomEventHandler Error;

    [DomName("onfocus")] event DomEventHandler Focused;

    [DomName("oninput")] event DomEventHandler Input;

    [DomName("oninvalid")] event DomEventHandler Invalid;

    [DomName("onkeydown")] event DomEventHandler KeyDown;

    [DomName("onkeypress")] event DomEventHandler KeyPress;

    [DomName("onkeyup")] event DomEventHandler KeyUp;

    [DomName("onload")] event DomEventHandler Loaded;

    [DomName("onloadeddata")] event DomEventHandler LoadedData;

    [DomName("onloadedmetadata")] event DomEventHandler LoadedMetadata;

    [DomName("onloadstart")] event DomEventHandler Loading;

    [DomName("onmousedown")] event DomEventHandler MouseDown;

    [DomLenientThis]
    [DomName("onmouseenter")]
    event DomEventHandler MouseEnter;

    [DomLenientThis]
    [DomName("onmouseleave")]
    event DomEventHandler MouseLeave;

    [DomName("onmousemove")] event DomEventHandler MouseMove;

    [DomName("onmouseout")] event DomEventHandler MouseOut;

    [DomName("onmouseover")] event DomEventHandler MouseOver;

    [DomName("onmouseup")] event DomEventHandler MouseUp;

    [DomName("onmousewheel")] event DomEventHandler MouseWheel;

    [DomName("onpause")] event DomEventHandler Paused;

    [DomName("onplay")] event DomEventHandler Played;

    [DomName("onplaying")] event DomEventHandler Playing;

    [DomName("onprogress")] event DomEventHandler Progress;

    [DomName("onratechange")] event DomEventHandler RateChanged;

    [DomName("onreset")] event DomEventHandler Resetted;

    [DomName("onresize")] event DomEventHandler Resized;

    [DomName("onscroll")] event DomEventHandler Scrolled;

    [DomName("onseeked")] event DomEventHandler Seeked;

    [DomName("onseeking")] event DomEventHandler Seeking;

    [DomName("onselect")] event DomEventHandler Selected;

    [DomName("onshow")] event DomEventHandler Shown;

    [DomName("onstalled")] event DomEventHandler Stalled;

    [DomName("onsubmit")] event DomEventHandler Submitted;

    [DomName("onsuspend")] event DomEventHandler Suspended;

    [DomName("ontimeupdate")] event DomEventHandler TimeUpdated;

    [DomName("ontoggle")] event DomEventHandler Toggled;

    [DomName("onvolumechange")] event DomEventHandler VolumeChanged;

    [DomName("onwaiting")] event DomEventHandler Waiting;
}