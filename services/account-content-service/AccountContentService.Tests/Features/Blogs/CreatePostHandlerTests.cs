using AccountContentService.Application.DTOs;
using AccountContentService.Application.Features.BlogPosts.Commands.CreatePost;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AccountContentService.Tests.Features.Blogs
{
    public class CreatePostHandlerTests
    {
        private readonly Mock<IBlogPostRepository> _repo = new();
        private readonly Mock<IUserProfileCache> _cache = new();
        private readonly Mock<IMapper> _mapper = new();

        private CreatePostHandler CreateHandler()
        {
            return new CreatePostHandler(
                _repo.Object,
                _cache.Object,
                _mapper.Object
            );
        }

        // =========================
        // ✅ SUCCESS
        // =========================
        [Fact]
        public async Task ShouldCreatePostSuccessfully()
        {
            var handler = CreateHandler();

            var command = new CreatePostCommand
            {
                UserId = Guid.NewGuid(),
                Title = "Test",
                ContentText = "Content"
            };

            var userProfile = new CachedUserProfile
            {
                FullName = "Minh",
                AvatarUrl = "avatar.jpg"
            };

            var post = new BlogPost();
            var postDto = new PostDto();

            _cache.Setup(x => x.GetProfileAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(userProfile);

            _mapper.Setup(x => x.Map<BlogPost>(command))
                .Returns(post);

            _mapper.Setup(x => x.Map<PostDto>(post))
                .Returns(postDto);

            var result = await handler.Handle(command, CancellationToken.None);

            result.Should().Be(postDto);

            _repo.Verify(x => x.AddAsync(It.IsAny<BlogPost>()), Times.Once);

            post.UserFullName.Should().Be("Minh");
            post.UserAvatarUrl.Should().Be("avatar.jpg");
        }

        // =========================
        // ❌ AVATAR NULL
        // =========================
        [Fact]
        public async Task ShouldSetEmptyAvatar_WhenAvatarIsNull()
        {
            var handler = CreateHandler();

            var command = new CreatePostCommand
            {
                UserId = Guid.NewGuid()
            };

            var post = new BlogPost();

            _cache.Setup(x => x.GetProfileAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachedUserProfile
                {
                    FullName = "Minh",
                    AvatarUrl = null
                });

            _mapper.Setup(x => x.Map<BlogPost>(command))
                .Returns(post);

            _mapper.Setup(x => x.Map<PostDto>(post))
                .Returns(new PostDto());

            await handler.Handle(command, CancellationToken.None);

            post.UserAvatarUrl.Should().Be(string.Empty);
        }

        // =========================
        // ❌ CACHE FAIL
        // =========================
        [Fact]
        public async Task ShouldThrow_WhenCacheFails()
        {
            var handler = CreateHandler();

            _cache.Setup(x => x.GetProfileAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Cache error"));

            await Assert.ThrowsAsync<Exception>(() =>
                handler.Handle(new CreatePostCommand(), CancellationToken.None));
        }

        // =========================
        // ❌ REPO FAIL
        // =========================
        [Fact]
        public async Task ShouldThrow_WhenRepositoryFails()
        {
            var handler = CreateHandler();

            var post = new BlogPost();

            _cache.Setup(x => x.GetProfileAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachedUserProfile
                {
                    FullName = "Minh"
                });

            _mapper.Setup(x => x.Map<BlogPost>(It.IsAny<CreatePostCommand>()))
                .Returns(post);

            _repo.Setup(x => x.AddAsync(It.IsAny<BlogPost>()))
                .ThrowsAsync(new Exception("DB error"));

            await Assert.ThrowsAsync<Exception>(() =>
                handler.Handle(new CreatePostCommand(), CancellationToken.None));
        }

        // =========================
        // ✅ VERIFY MAPPING
        // =========================
        [Fact]
        public async Task ShouldMapCommandToEntityCorrectly()
        {
            var handler = CreateHandler();

            var command = new CreatePostCommand
            {
                UserId = Guid.NewGuid(),
                Title = "Hello"
            };

            var post = new BlogPost();

            _cache.Setup(x => x.GetProfileAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CachedUserProfile
                {
                    FullName = "Minh"
                });

            _mapper.Setup(x => x.Map<BlogPost>(command))
                .Returns(post);

            _mapper.Setup(x => x.Map<PostDto>(post))
                .Returns(new PostDto());

            await handler.Handle(command, CancellationToken.None);

            _mapper.Verify(x => x.Map<BlogPost>(command), Times.Once);
        }
    }
}