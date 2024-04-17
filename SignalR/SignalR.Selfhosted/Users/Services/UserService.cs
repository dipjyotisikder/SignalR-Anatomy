﻿using Microsoft.IdentityModel.Tokens;
using SignalR.Common.Constants;
using SignalR.SelfHosted.Notification.Services;
using SignalR.SelfHosted.Notifications.Services;
using SignalR.SelfHosted.Users.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.SelfHosted.Users.Services;

public class UserService : IUserService
{
    private readonly IDataContext _context;
    private readonly IHubService _hubService;
    private readonly ICurrentUser _currentUser;

    public UserService(IDataContext context, IHubService hubService, ICurrentUser currentUser)
    {
        _context = context;
        _hubService = hubService;
        _currentUser = currentUser;
    }

    public TokenModel CreateUser(CreateUserRequest request)
    {
        var user = _context.Users.FirstOrDefault(x => x.FullNameNormalized == request.FullName.ToUpper());
        if (user == null)
        {
            user = new User(id: _context.Users.Count + 1, fullName: request.FullName, photoUrl: request.PhotoUrl);
            _context.Users.Add(user);
        }

        var refreshToken = Guid.NewGuid().ToString();
        var token = new Token
        {
            RefreshToken = refreshToken,
            TokenUserId = user.Id,
        };
        token.SetDefaultExpiryDate();

        _context.Tokens.Add(token);

        return PrepareToken(user, refreshToken);
    }

    public IEnumerable<User> GetUsers()
    {
        return _context.Users;
    }

    public async Task OnLineUser(bool onLine, int userId)
    {
        var user = _context.Users.Where(x => x.Id == userId).FirstOrDefault();
        if (user is not null)
        {
            user.OnLine = onLine;
        }

        var conversationIds = _context.ConversationAudiences
            .Where(x => x.AudienceUserId == userId)
            .Select(x => x.ConversationId).Distinct();

        var audienceUserIds = _context.ConversationAudiences
            .Where(x => conversationIds.Contains(x.ConversationId))
            .Select(x => x.AudienceUserId.ToString()).Distinct();

        await _hubService.SendToGroupsAsync(
            groups: audienceUserIds,
            eventName: HubEventName.Create(HubConstants.Events.USER_IS_ONLINE),
            userId);
    }

    public User UpdateUser(UpdateUserRequest request)
    {
        var user = _context.Users.Where(x => x.Id == request.Id).FirstOrDefault();
        if (user == null)
        {
            return null;
        }

        user.SetFullName(request.FullName);
        user.PhotoUrl = request.PhotoUrl;
        user.OnLine = request.Active;

        return user;
    }

    public TokenModel RefreshUserToken(RefreshUserTokenRequest request)
    {
        var token = _context.Tokens.FirstOrDefault(x => x.RefreshToken == request.RefreshToken);
        if (token == null)
        {
            return null;
        }

        var user = _context.Users.FirstOrDefault(x => x.Id == token.TokenUserId);
        if (user == null)
        {
            return null;
        }

        var refreshToken = Guid.NewGuid().ToString();

        token.RefreshToken = refreshToken;
        token.SetDefaultExpiryDate();

        return PrepareToken(user, refreshToken);
    }

    private static TokenModel PrepareToken(User user, string refreshToken)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthenticationConstants.TOKEN_SECRET_KEY));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: AuthenticationConstants.ISSUER,
            audience: AuthenticationConstants.AUDIENCE,
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName)
            },
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new TokenModel
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken
        };
    }

    public Task TriggerUserIsTypingEvent(int conversationId)
    {
        var audienceUserIds = _context.ConversationAudiences
            .Where(x => x.ConversationId == conversationId && x.AudienceUserId != _currentUser.Id)
            .Select(x => x.AudienceUserId.ToString()).Distinct();

        var typingUser = _context.Users
           .Where(x => x.Id == _currentUser.Id)
           .Select(x => new UserModel
           {
               Id = x.Id,
               FullName = x.FullName,
               OnLine = x.OnLine,
               PhotoUrl = x.PhotoUrl,
           }).FirstOrDefault();

        return _hubService.SendToGroupsAsync(
                    groups: audienceUserIds,
                    eventName: HubEventName.Create(HubConstants.Events.USER_IS_TYPING),
                    typingUser);
    }
}
