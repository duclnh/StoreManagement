using DistributedSystem.Constract.Abstraction.Message;
using FluentAssertions;
using NetArchTest.Rules;

namespace DistributedSystem.Test
{
    public class ArchitectureTests
    {
        private const string DomainNamespace = "DistributedSystem.Domain";
        private const string ApiNamespace = "DistributedSystem.Apin";
        private const string ApplicationNamespace = "DistributedSystem.Application";
        private const string InfrastructureNamespace = "DistributedSystem.Infrastructure";
        private const string PersistenceNamespace = "DistributedSystem.Persistence";
        private const string PresentationNamespace = "DistributedSystem.Presentation";


        [Fact]
        public void Domain_Should_Not_HaveDepedencyOnOtherProjects()
        {
            //Arreage
            var assembly = Domain.AssemblyReference.Assembly;

            var otherProjects = new[] {
                ApplicationNamespace,
                ApiNamespace,
                InfrastructureNamespace,
                PersistenceNamespace
            };

            //Act
            var testResult = Types.InAssembly(assembly)
                                    .ShouldNot()
                                    .HaveDependencyOnAny(otherProjects)
                                    .GetResult();

            //Assert

            testResult.IsSuccessful.Should().BeTrue();
        }


        [Fact]
        public void Persistence_Should_Not_HaveDepedencyOnOtherProjects()
        {
            //Arreage
            var assembly = Persistence.AssemblyReference.Assembly;

            var otherProjects = new[] {
                ApplicationNamespace,
                ApiNamespace,
                InfrastructureNamespace,
                PresentationNamespace
            };

            //Act
            var testResult = Types.InAssembly(assembly)
                                    .ShouldNot()
                                    .HaveDependencyOnAny(otherProjects)
                                    .GetResult();

            //Assert

            testResult.IsSuccessful.Should().BeTrue();
        }



        [Fact]
        public void Application_Should_Not_HaveDepedencyOnOtherProjects()
        {
            //Arreage
            var assembly = Application.AssemblyReference.Assembly;

            var otherProjects = new[] {
                ApiNamespace,
                PresentationNamespace
            };

            //Act
            var testResult = Types.InAssembly(assembly)
                                    .ShouldNot()
                                    .HaveDependencyOnAny(otherProjects)
                                    .GetResult();

            //Assert

            testResult.IsSuccessful.Should().BeTrue();
        }


        [Fact]
        public void Infrastructure_Should_Not_HaveDepedencyOnOtherProjects()
        {
            //Arreage
            var assembly = Infrastructure.AssemblyReference.Assembly;

            var otherProjects = new[] {
                ApiNamespace,
                PresentationNamespace
            };

            //Act
            var testResult = Types.InAssembly(assembly)
                                    .ShouldNot()
                                    .HaveDependencyOnAny(otherProjects)
                                    .GetResult();

            //Assert

            testResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Presentation_Should_Not_HaveDepedencyOnOtherProjects()
        {
            //Arreage
            var assembly = Presentation.AssemblyReference.Assembly;

            var otherProjects = new[] {
                ApiNamespace,
                InfrastructureNamespace,
                DomainNamespace,
                PersistenceNamespace,
            };

            //Act
            var testResult = Types.InAssembly(assembly)
                                    .ShouldNot()
                                    .HaveDependencyOnAny(otherProjects)
                                    .GetResult();

            //Assert

            testResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void Command_Should_Having_NamingConventionEndingCommand()
        {
            var assembly = Constract.AssemblyReference.Assembly;


            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(ICommand))
                                  .And()
                                  .DoNotImplementInterface(typeof(IDomainEvent))
                                  .Should()
                                  .HaveNameEndingWith("Command")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void CommandT_Should_Having_NamingConventionEndingCommand()
        {
            var assembly = Constract.AssemblyReference.Assembly;


            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(ICommand<>))
                                  .And()
                                  .DoNotImplementInterface(typeof(IDomainEvent))
                                  .Should()
                                  .HaveNameEndingWith("Command")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }



        //[Fact]
        //public void Command_Should_Have_BeSealed()
        //{
        //    var assembly = Constract.AssemblyReference.Assembly;

        //    var testResult = Types.InAssembly(assembly)
        //                       .That()
        //                       .ImplementInterface(typeof(ICommand))
        //                          .And()
        //                          .DoNotImplementInterface(typeof(IDomainEvent))
        //                       .Should()
        //                       .BeSealed()
        //                       .GetResult();

        //    testResult.IsSuccessful.Should().BeTrue();
        //}

        //[Fact]
        //public void CommandT_Should_Have_BeSealed()
        //{
        //    var assembly = Constract.AssemblyReference.Assembly;

        //    var testResult = Types.InAssembly(assembly)
        //                       .That()
        //                       .ImplementInterface(typeof(ICommand<>))
        //                          .And()
        //                          .DoNotImplementInterface(typeof(IDomainEvent))
        //                       .Should()
        //                       .BeSealed()
        //                       .GetResult();

        //    testResult.IsSuccessful.Should().BeTrue();
        //}


        [Fact]
        public void CommandHandler_Should_Having_NamingConventionEndingCommandHandler()
        {
            var assembly = Application.AssemblyReference.Assembly;


            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(ICommandHandler<,>))
                                  .Should()
                                  .HaveNameEndingWith("CommandHandler")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void CommandHandlerT_Should_Having_NamingConventionEndingCommandHandler()
        {
            var assembly = Application.AssemblyReference.Assembly;


            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(ICommandHandler<>))
                                  .Should()
                                  .HaveNameEndingWith("CommandHandler")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void CommandHandler_Should_Having_NamingConventionEndingWhenProjectChangeHandler()
        {
            var assembly = Infrastructure.AssemblyReference.Assembly;


            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(ICommandHandler<,>))
                                  .Should()
                                  .HaveNameEndingWith("WhenProjectChangeHandler")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }


        #region Query
        [Fact]
        public void Query_Should_Having_NamingConventionEndingQuery()
        {
            const string namespaceException = "DistributedSystem.Constract.Services.V1.Identity";

            var assembly = Constract.AssemblyReference.Assembly;

            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(IQuery<>))
                                  .And()
                                  .DoNotResideInNamespace(namespaceException)
                                  .Should()
                                  .HaveNameEndingWith("Query")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void QueryHandler_Should_Having_NamingConventionEndingQueryHandler()
        {
            var assembly = Application.AssemblyReference.Assembly;


            var testResult = Types.InAssembly(assembly)
                                  .That()
                                  .ImplementInterface(typeof(IQueryHandler<,>))
                                  .Should()
                                  .HaveNameEndingWith("QueryHandler")
                                  .GetResult();

            testResult.IsSuccessful.Should().BeTrue();
        }

        #endregion

    }
}