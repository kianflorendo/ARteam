using UnityEngine;

public enum DeviceTilt { Portrait, TiltLeft, TiltRight }

// Reads the accelerometer directly so UI panels can rotate to match how the
// player is physically holding the phone, independent of the OS's actual
// window/render-surface orientation.
//
// Why not Screen.orientation / Screen.width vs Screen.height: on this project,
// switching AndroidManifest + Player Settings to allow OS-level landscape
// rotation left Unity's render surface stuck at its old portrait pixel
// dimensions (a GameActivity + configChanges quirk) -- the OS window rotated
// but the game content stayed portrait-sized and got letterboxed inside it.
// Reading raw tilt from the accelerometer sidesteps that entirely: the app
// keeps rendering at a fixed portrait resolution, and panels are rotated in
// place to compensate for how the player is actually holding the device. This
// also keeps working even if a device's system-wide auto-rotate is off, since
// it never depends on the OS's orientation state at all.
public static class DeviceTiltHelper
{
    private const float TiltThreshold = 0.55f;

    public static DeviceTilt GetTilt()
    {
        var a = Input.acceleration;
        if (a.x > TiltThreshold) return DeviceTilt.TiltRight;
        if (a.x < -TiltThreshold) return DeviceTilt.TiltLeft;
        return DeviceTilt.Portrait;
    }
}
