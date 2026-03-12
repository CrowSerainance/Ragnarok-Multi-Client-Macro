using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

public sealed class ClientBindingService
{
    private readonly ClientDiscoveryService _discoveryService;
    private readonly ProcessAttachmentService _attachmentService;

    public ClientBindingService(ClientDiscoveryService discoveryService, ProcessAttachmentService attachmentService)
    {
        _discoveryService = discoveryService;
        _attachmentService = attachmentService;
    }
    public void BindProfile(ClientProfile profile, ClientWindowRef liveWindow)
    {
        bool attached = _attachmentService.Attach(liveWindow.ProcessId, new IntPtr(liveWindow.WindowHandle));
        profile.BoundWindow = CloneWindow(liveWindow);
        ApplyRuntimeStatus(profile, new ClientWindowMatchResult(
            ClientWindowMatchKind.ExactHandle,
            liveWindow,
            true,
            false,
            "Live",
            $"Bound to {liveWindow.DisplayText}. " + (attached ? "[Tight Connection]" : $"[External: {_attachmentService.LastAttachFailure}]")));
    }

    public void ClearBinding(ClientProfile profile)
    {
        profile.BoundWindow = null;
        _attachmentService.Detach();
        ApplyRuntimeStatus(profile, ClientWindowMatcher.Match(null, []));
    }

    public ClientWindowMatchResult GetResolution(ClientProfile profile, IReadOnlyList<ClientWindowRef>? windows = null, bool updateProfile = true)
    {
        windows ??= _discoveryService.DiscoverWindows();
        var result = ClientWindowMatcher.Match(profile.BoundWindow, windows);
        if (updateProfile && result.ResolvedWindow is not null && result.WasRebound)
        {
            profile.BoundWindow = CloneWindow(result.ResolvedWindow);
        }

        ApplyRuntimeStatus(profile, result);
        return result;
    }

    public ClientWindowRef? ResolveLiveWindow(ClientProfile profile)
    {
        var result = GetResolution(profile, updateProfile: true);
        if (result.ResolvedWindow is not null)
            return result.ResolvedWindow;

        // Fallback only when the attached process still matches this profile.
        if (_attachmentService.IsAttached
            && _attachmentService.WindowHandle != IntPtr.Zero
            && profile.BoundWindow is not null
            && profile.BoundWindow.ProcessId == _attachmentService.ProcessId)
        {
            var attached = profile.BoundWindow ?? new ClientWindowRef();
            var freshRef = new ClientWindowRef
            {
                WindowHandle = _attachmentService.WindowHandle.ToInt64(),
                ProcessId = _attachmentService.ProcessId,
                ProcessName = attached.ProcessName,
                WindowTitle = attached.WindowTitle,
                ClientWidth = attached.ClientWidth > 0 ? attached.ClientWidth : 800,
                ClientHeight = attached.ClientHeight > 0 ? attached.ClientHeight : 600,
            };
            return freshRef;
        }

        return null;
    }

    private static ClientWindowRef CloneWindow(ClientWindowRef liveWindow)
    {
        return new ClientWindowRef
        {
            WindowHandle = liveWindow.WindowHandle,
            ProcessId = liveWindow.ProcessId,
            ProcessName = liveWindow.ProcessName,
            WindowTitle = liveWindow.WindowTitle,
            ClientWidth = liveWindow.ClientWidth,
            ClientHeight = liveWindow.ClientHeight,
        };
    }

    private static void ApplyRuntimeStatus(ClientProfile profile, ClientWindowMatchResult result)
    {
        profile.RuntimeStatusLabel = result.Label;
        profile.RuntimeStatusDetail = result.Detail;
        profile.HasLiveWindow = result.IsLive;
    }
}
