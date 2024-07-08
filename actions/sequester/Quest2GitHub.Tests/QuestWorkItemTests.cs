using Quest2GitHub.Models;

namespace Quest2GitHub.Tests;

public class QuestWorkItemTests
{
    [Theory]
    [InlineData("short title")]
    [InlineData("A really long title, in fact, it's so long that we know it will exceed the max length of 255 characters imposed by the Azure DevOps API, thus proving our point, and being truncated in the object's .ctor. Isn't programming fun?! Wow, this title is really long...I hope we're done typing soon.")]
    public void QuestItemConstructorEnsuresTitleIsTruncated(string title)
    {
        var sut = new QuestWorkItem
        {
            Id = 777,
            ParentWorkItemId = 100,
            ParentRelationIndex = 1,
            Title = title, // Truncated on init
            State = "Wisconsin 🧀",
            Description = "Test description",
            AreaPath = "Test/Area",
            IterationPath = "Test/Path",
            AssignedToId = Guid.NewGuid(),
            StoryPoints = 1,
            Tags = []
        };

        Assert.True(sut.Title.Length < 256);
    }
}
