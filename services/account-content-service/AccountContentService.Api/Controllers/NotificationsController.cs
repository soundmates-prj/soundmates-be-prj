using AccountContentService.Api.Common;
using AccountContentService.Api.Constants;
using AccountContentService.Api.Contracts.Responses;
using AccountContentService.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using AccountContentService.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using AccountContentService.Application.Features.Notifications.Queries.GetNotifications;
using AccountContentService.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountContentService.Api.Controllers
{
    /// <summary>
    /// Handles all notification-related operations such as retrieving notifications
    /// and updating their read status for the current authenticated user.
    /// </summary>
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        /// <summary>
        /// Initializes a new instance of <see cref="NotificationsController"/>.
        /// </summary>
        /// <param name="mediator">Mediator instance for handling CQRS requests.</param>
        /// <param name="mapper"></param>
        public NotificationsController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for the current user.
        /// </summary>
        /// <param name="userId">User following retrive notifications</param>
        /// <param name="page">Page number (default is 1).</param>
        /// <param name="pageSize">Number of items per page (default is 10).</param>
        /// <returns>
        /// A paginated list of notifications including read/unread status.
        /// </returns>
        /// <remarks>
        /// This API is used to display notifications in the notification center.
        /// Supports pagination for performance optimization.
        /// </remarks>
        /// <response code="200">Returns the list of notifications.</response>
        /// <response code="401">Unauthorized - user is not authenticated.</response>
        [HttpGet(ApiRoutes.Users.GetUserNotifications)]
        public async Task<IActionResult> GetNotifications(
            [FromRoute] Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new GetNotificationsQuery
            {
                UserId = userId,
                Page = page,
                PageSize = pageSize
            });
            var items = _mapper.Map<IEnumerable<NotificationResponse>>(result.Items);

            var response = new PaginationResponse<NotificationResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<NotificationResponse>>.Ok(response, "Get notifications successfully"));
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for the current user.
        /// </summary>
        /// <param name="page">Page number (default is 1).</param>
        /// <param name="pageSize">Number of items per page (default is 10).</param>
        /// <returns>
        /// A paginated list of notifications including read/unread status.
        /// </returns>
        /// <remarks>
        /// This API is used to display notifications in the notification center.
        /// Supports pagination for performance optimization.
        /// </remarks>
        /// <response code="200">Returns the list of notifications.</response>
        /// <response code="401">Unauthorized - user is not authenticated.</response>
        [HttpGet(ApiRoutes.Me.MyNotifications)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var result = await _mediator.Send(new GetNotificationsQuery
            {
                UserId = userId,
                Page = page,
                PageSize = pageSize
            });
            var items = _mapper.Map<IEnumerable<NotificationResponse>>(result.Items);

            var response = new PaginationResponse<NotificationResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<NotificationResponse>>.Ok(response, "Get notifications successfully"));
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for the current user.
        /// </summary>
        /// <param name="page">Page number (default is 1).</param>
        /// <param name="pageSize">Number of items per page (default is 10).</param>
        /// <returns>
        /// A paginated list of read notifications.
        /// </returns>
        /// <remarks>
        /// This API is used to display notifications in the notification center.
        /// Supports pagination for performance optimization.
        /// </remarks>
        /// <response code="200">Returns the list of notifications.</response>
        /// <response code="401">Unauthorized - user is not authenticated.</response>
        [HttpGet(ApiRoutes.Me.MyReadNotifications)]
        public async Task<IActionResult> GetReadNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var result = await _mediator.Send(new GetReadNotificationsQuery
            {
                UserId = userId,
                Page = page,
                PageSize = pageSize
            });
            var items = _mapper.Map<IEnumerable<NotificationResponse>>(result.Items);

            var response = new PaginationResponse<NotificationResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<NotificationResponse>>.Ok(response, "Get notifications successfully"));
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for the current user.
        /// </summary>
        /// <param name="page">Page number (default is 1).</param>
        /// <param name="pageSize">Number of items per page (default is 10).</param>
        /// <returns>
        /// A paginated list of not read notifications
        /// </returns>
        /// <remarks>
        /// This API is used to display notifications in the notification center.
        /// Supports pagination for performance optimization.
        /// </remarks>
        /// <response code="200">Returns the list of notifications.</response>
        /// <response code="401">Unauthorized - user is not authenticated.</response>
        [HttpGet(ApiRoutes.Me.MyNotReadNotifications)]
        public async Task<IActionResult> GetNotReadNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = UserContext.GetUserId(HttpContext);
            var result = await _mediator.Send(new GetNotReadNotificationsQuery
            {
                UserId = userId,
                Page = page,
                PageSize = pageSize
            });
            var items = _mapper.Map<IEnumerable<NotificationResponse>>(result.Items);

            var response = new PaginationResponse<NotificationResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount);

            return Ok(ApiResponse<PaginationResponse<NotificationResponse>>.Ok(response, "Get notifications successfully"));
        }

        /// <summary>
        /// Marks a specific notification as read.
        /// </summary>
        /// <param name="notificationId">The unique identifier of the notification.</param>
        /// <returns>
        /// True if the notification was successfully marked as read; otherwise false.
        /// </returns>
        /// <remarks>
        /// This API updates the read status of a single notification.
        /// </remarks>
        /// <response code="200">Notification marked as read successfully.</response>
        /// <response code="400">Invalid notification ID.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="404">Notification not found.</response>
        [HttpPut(ApiRoutes.Notifications.Read)]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid notificationId)
        {
            var result = await _mediator.Send(new MarkNotificationAsReadCommand(notificationId));

            return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
        }

        /// <summary>
        /// Marks all notifications of the current user as read.
        /// </summary>
        /// <returns>
        /// True if all notifications were successfully marked as read.
        /// </returns>
        /// <remarks>
        /// This API is typically used when the user clicks "Mark all as read"
        /// in the notification center.
        /// </remarks>
        /// <response code="200">All notifications marked as read successfully.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPut(ApiRoutes.Notifications.ReadAll)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = UserContext.GetUserId(HttpContext);
            var result = await _mediator.Send(new MarkAllNotificationsAsReadCommand(userId));

            return Ok(ApiResponse<bool>.Ok(result, $"Status: {result}"));
        }
    }
}