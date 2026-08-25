using FluentAssertions;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace PhuXuanParkingSystem.Tests.DependencyInjection
{
    public class DiContainerTests
    {
        private readonly IServiceProvider _serviceProvider;

        public DiContainerTests()
        {
            var services = new ServiceCollection();

            // Database Context (Singleton)
            services.AddSingleton(MongoDbContext.Instance);

            // Generic Repository bao quát toàn bộ Entities kế thừa BaseEntity
            services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

            // Forms
            services.AddTransient<FrmMain>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void DiContainer_ShouldResolve_MongoDbContextAsSingleton()
        {
            // Act
            var context1 = _serviceProvider.GetService<MongoDbContext>();
            var context2 = _serviceProvider.GetService<MongoDbContext>();

            // Assert
            context1.Should().NotBeNull();
            context2.Should().NotBeNull();
            context1.Should().BeSameAs(context2);
        }

        [Theory]
        [InlineData(typeof(IRepository<ParkingSession>))]
        [InlineData(typeof(IRepository<Vehicle>))]
        [InlineData(typeof(IRepository<Person>))]
        [InlineData(typeof(IRepository<Department>))]
        [InlineData(typeof(IRepository<Company>))]
        [InlineData(typeof(IRepository<Contractor>))]
        [InlineData(typeof(IRepository<Lane>))]
        [InlineData(typeof(IRepository<Device>))]
        [InlineData(typeof(IRepository<User>))]
        public void DiContainer_ShouldResolve_GenericRepositoriesForAllEntities(Type repositoryType)
        {
            // Act
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetService(repositoryType);

            // Assert
            repo.Should().NotBeNull();
        }
    }
}
