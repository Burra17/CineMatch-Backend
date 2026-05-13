using CineMatch.Application.Interfaces;
using CineMatch.Infrastructure.Services;
using NSubstitute;

namespace CineMatch.Tests.Services
{
    [TestFixture]
    public class JoinCodeGeneratorTests
    {
        private const string AllowedAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int ExpectedCodeLength = 6;
        private const int MaxAttempts = 10;

        private IWatchPartyRepository _watchPartyRepositoryMock;
        private JoinCodeGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _watchPartyRepositoryMock = Substitute.For<IWatchPartyRepository>();
            _generator = new JoinCodeGenerator(_watchPartyRepositoryMock);
        }

        [Test]
        public async Task GenerateUniqueCodeAsync_RepositoryReturnsUnique_ReturnsCode()
        {
            // Arrange
            // Setup: Första genererade koden är unik i databasen
            _watchPartyRepositoryMock
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var code = await _generator.GenerateUniqueCodeAsync(CancellationToken.None);

            // Assert
            Assert.That(code, Is.Not.Null.And.Not.Empty, "Generatorn ska returnera en kod.");
            await _watchPartyRepositoryMock.Received(1)
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task GenerateUniqueCodeAsync_FirstAttemptCollides_RetriesAndSucceeds()
        {
            // Arrange
            // Setup: Första försöket kolliderar (false), andra lyckas (true)
            _watchPartyRepositoryMock
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false, true);

            // Act
            var code = await _generator.GenerateUniqueCodeAsync(CancellationToken.None);

            // Assert
            Assert.That(code, Is.Not.Null.And.Not.Empty, "Generatorn ska returnera en kod efter retry.");
            // Verifiera att vi gick igenom exakt 2 attempts (collision + success)
            await _watchPartyRepositoryMock.Received(2)
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public void GenerateUniqueCodeAsync_AlwaysCollides_ThrowsAfterMaxAttempts()
        {
            // Arrange
            // Setup: Alla anrop returnerar false — koden är aldrig unik
            _watchPartyRepositoryMock
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            // Act + Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _generator.GenerateUniqueCodeAsync(CancellationToken.None),
                $"Generatorn ska kasta efter {MaxAttempts} misslyckade försök.");

            // Verifiera att vi faktiskt försökte MaxAttempts gånger innan vi gav upp
            _watchPartyRepositoryMock.Received(MaxAttempts)
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task GenerateUniqueCodeAsync_GeneratedCode_HasLength6()
        {
            // Arrange
            _watchPartyRepositoryMock
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var code = await _generator.GenerateUniqueCodeAsync(CancellationToken.None);

            // Assert
            Assert.That(code.Length, Is.EqualTo(ExpectedCodeLength), "Join-koden ska vara exakt 6 tecken.");
        }

        [Test]
        public async Task GenerateUniqueCodeAsync_GeneratedCode_OnlyContainsAllowedAlphabet()
        {
            // Arrange
            _watchPartyRepositoryMock
                .IsJoinCodeUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);

            // Act
            var code = await _generator.GenerateUniqueCodeAsync(CancellationToken.None);

            // Assert
            // Verifiera att varje tecken är från det tillåtna alfabetet (inga förväxlingsbara O/0/I/1/l)
            Assert.That(
                code.All(c => AllowedAlphabet.Contains(c)),
                Is.True,
                $"Koden '{code}' innehåller tecken utanför det tillåtna alfabetet '{AllowedAlphabet}'.");
        }
    }
}
