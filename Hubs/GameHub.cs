using GameCoTuong.Common;
using GameCoTuong.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AuthorizationResult = GameCoTuong.DTOs.AuthorizationResult;

namespace GameCoTuong.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IRoomAuthorizationService _authzService;

    public GameHub(IRoomAuthorizationService authzService)
    {
        _authzService = authzService;
    }

    /// <summary>
    /// Tham gia phòng cờ: Chỉ cho phép khi có token hợp lệ và là thành viên
    /// </summary>
    public async Task<AuthorizationResult> JoinRoom(int roomId, int? claimedUserId = null)
    {
        // 1. Chặn giả mạo mã người dùng
        var impersonationCheck = _authzService.VerifyNoImpersonation(Context.User, claimedUserId);
        if (!impersonationCheck.IsAuthorized)
        {
            return impersonationCheck;
        }

        var userId = Context.User.GetUserId()!.Value;

        // 2. Kiểm tra thành viên phòng
        var authResult = await _authzService.AuthorizeMembershipAsync(roomId, userId);
        if (!authResult.IsAuthorized)
        {
            return authResult;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
        return AuthorizationResult.Success($"Đã vào phòng cờ {roomId}.");
    }

    /// <summary>
    /// Bắt đầu ván đấu: Kiểm tra quyền thành viên và trạng thái phòng
    /// </summary>
    public async Task<AuthorizationResult> StartMatch(int roomId, int? claimedUserId = null)
    {
        var impersonationCheck = _authzService.VerifyNoImpersonation(Context.User, claimedUserId);
        if (!impersonationCheck.IsAuthorized)
        {
            return impersonationCheck;
        }

        var userId = Context.User.GetUserId()!.Value;
        var authResult = await _authzService.AuthorizeStartMatchAsync(roomId, userId);
        if (!authResult.IsAuthorized)
        {
            return authResult;
        }

        await Clients.Group($"room_{roomId}").SendAsync("MatchStarted", new { RoomId = roomId, StartedBy = userId });
        return AuthorizationResult.Success("Ván đấu đã bắt đầu.");
    }

    /// <summary>
    /// Đi quân cờ: Chỉ đấu thủ trong ván cờ mới được đi quân, khán giả bị từ chối
    /// </summary>
    public async Task<AuthorizationResult> MakeMove(int roomId, int matchId, string moveNotation, int? claimedUserId = null)
    {
        var impersonationCheck = _authzService.VerifyNoImpersonation(Context.User, claimedUserId);
        if (!impersonationCheck.IsAuthorized)
        {
            return impersonationCheck;
        }

        var userId = Context.User.GetUserId()!.Value;
        var authResult = await _authzService.AuthorizeMakeMoveAsync(roomId, matchId, userId);
        if (!authResult.IsAuthorized)
        {
            return authResult;
        }

        await Clients.Group($"room_{roomId}").SendAsync("MoveMade", new { MatchId = matchId, UserId = userId, Notation = moveNotation });
        return AuthorizationResult.Success("Nước đi hợp lệ đã được thực hiện.");
    }

    /// <summary>
    /// Đóng phòng cờ: Chỉ thành viên có quyền mới được đóng phòng
    /// </summary>
    public async Task<AuthorizationResult> CloseRoom(int roomId, int? claimedUserId = null)
    {
        var impersonationCheck = _authzService.VerifyNoImpersonation(Context.User, claimedUserId);
        if (!impersonationCheck.IsAuthorized)
        {
            return impersonationCheck;
        }

        var userId = Context.User.GetUserId()!.Value;
        var authResult = await _authzService.AuthorizeCloseRoomAsync(roomId, userId);
        if (!authResult.IsAuthorized)
        {
            return authResult;
        }

        await Clients.Group($"room_{roomId}").SendAsync("RoomClosed", new { RoomId = roomId, ClosedBy = userId });
        return AuthorizationResult.Success("Phòng cờ đã được đóng.");
    }

    /// <summary>
    /// Gọi video / WebRTC Signaling: Người ngoài phòng bị chặn 100%
    /// </summary>
    public async Task<AuthorizationResult> SendVideoSignal(int roomId, string signalData, int? claimedUserId = null)
    {
        var impersonationCheck = _authzService.VerifyNoImpersonation(Context.User, claimedUserId);
        if (!impersonationCheck.IsAuthorized)
        {
            return impersonationCheck;
        }

        var userId = Context.User.GetUserId()!.Value;
        var authResult = await _authzService.AuthorizeVideoCallAsync(roomId, userId);
        if (!authResult.IsAuthorized)
        {
            return authResult;
        }

        await Clients.OthersInGroup($"room_{roomId}").SendAsync("ReceiveVideoSignal", new { FromUserId = userId, Signal = signalData });
        return AuthorizationResult.Success("Tín hiệu video đã được gửi.");
    }
}
