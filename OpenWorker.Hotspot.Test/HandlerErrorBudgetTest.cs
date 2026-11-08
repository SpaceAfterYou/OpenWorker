using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Handler;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class HandlerErrorBudgetTest
{
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    private static HandlerErrorBudget Create(FakeTimeProvider time, int maxErrors = 10) =>
        new(time, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = maxErrors, Window = Minute }));

    [TestMethod]
    public void NineErrorsStayWithinTheBudgetAndTheTenthDoesNot()
    {
        var budget = Create(new FakeTimeProvider());
        var state = new HandlerErrorsComponent();

        for (var error = 1; error <= 9; error++)
        {
            var verdict = budget.Record(ref state);

            Assert.AreEqual(error, verdict.Count);
            Assert.IsFalse(verdict.Exceeded, $"Error {error} is within the budget.");
            Assert.IsFalse(state.Closed);
        }

        var tenth = budget.Record(ref state);

        Assert.AreEqual(10, tenth.Count);
        Assert.IsTrue(tenth.Exceeded);
        Assert.IsTrue(state.Closed);
    }

    [TestMethod]
    public void OnlyTheFirstErrorOfAWindowIsTheFirst()
    {
        var budget = Create(new FakeTimeProvider());
        var state = new HandlerErrorsComponent();

        Assert.IsTrue(budget.Record(ref state).IsFirst);
        Assert.IsFalse(budget.Record(ref state).IsFirst);
        Assert.IsFalse(budget.Record(ref state).IsFirst);
    }

    [TestMethod]
    public void TheCountStartsOverWhenTheWindowHasPassed()
    {
        var time = new FakeTimeProvider();
        var budget = Create(time);
        var state = new HandlerErrorsComponent();

        for (var error = 0; error < 9; error++)
        {
            budget.Record(ref state);
        }

        time.Advance(Minute);

        var again = budget.Record(ref state);

        Assert.AreEqual(1, again.Count);
        Assert.IsTrue(again.IsFirst);
        Assert.IsFalse(again.Exceeded);

        // Nine more fill the second window up to the limit and no further.
        for (var error = 2; error <= 9; error++)
        {
            Assert.IsFalse(budget.Record(ref state).Exceeded);
        }

        Assert.IsTrue(budget.Record(ref state).Exceeded);
    }

    [TestMethod]
    public void ALaterErrorDoesNotMoveTheEndOfTheWindow()
    {
        var time = new FakeTimeProvider();
        var budget = Create(time);
        var state = new HandlerErrorsComponent();

        budget.Record(ref state);

        time.Advance(TimeSpan.FromSeconds(40));

        Assert.AreEqual(2, budget.Record(ref state).Count);

        // A minute after the first error, though only twenty seconds after the second.
        time.Advance(TimeSpan.FromSeconds(20));

        var verdict = budget.Record(ref state);

        Assert.AreEqual(1, verdict.Count);
        Assert.IsTrue(verdict.IsFirst);
    }

    [TestMethod]
    public void ErrorsJustInsideTheWindowAreCounted()
    {
        var time = new FakeTimeProvider();
        var budget = Create(time);
        var state = new HandlerErrorsComponent();

        budget.Record(ref state);

        time.Advance(Minute - TimeSpan.FromMilliseconds(1));

        Assert.AreEqual(2, budget.Record(ref state).Count);
    }

    [TestMethod]
    public void TheLimitCanBeOne()
    {
        var budget = Create(new FakeTimeProvider(), maxErrors: 1);
        var state = new HandlerErrorsComponent();

        Assert.IsTrue(budget.Record(ref state).Exceeded);
    }

    [TestMethod]
    public void ALimitBelowOneIsRefused()
    {
        var time = new FakeTimeProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() => Create(time, maxErrors: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HandlerErrorBudget(time, Options.Create(new HandlerErrorBudgetOptions { MaxErrors = 10, Window = TimeSpan.Zero })));
    }

    [TestMethod]
    public void APacketThatCannotBeReadIsMalformed()
    {
        Exception[] malformed =
        [
            new EndOfStreamException(),
            new IOException(),
            new ArgumentOutOfRangeException("count"),
            new InvalidDataException()
        ];

        foreach (var exception in malformed)
        {
            Assert.AreEqual(HandlerErrorReason.Malformed, HandlerErrorBudget.Classify(exception), exception.GetType().Name);
        }
    }

    [TestMethod]
    public void AnyOtherExceptionIsAHandlerFault()
    {
        Exception[] faults =
        [
            new InvalidOperationException(),
            new FormatException(),
            new KeyNotFoundException(),
            new TimeoutException()
        ];

        foreach (var exception in faults)
        {
            Assert.AreEqual(HandlerErrorReason.HandlerFault, HandlerErrorBudget.Classify(exception), exception.GetType().Name);
        }
    }

    [TestMethod]
    public void TheReasonsHaveTheTagsOfTheMetric()
    {
        Assert.AreEqual("malformed", HandlerErrorBudget.Tag(HandlerErrorReason.Malformed));
        Assert.AreEqual("handler_fault", HandlerErrorBudget.Tag(HandlerErrorReason.HandlerFault));
    }
}
