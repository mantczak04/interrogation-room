using NUnit.Framework;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI.Tests
{
    public sealed class UiControlStatesTests
    {
        [Test]
        public void NormalizeMakesStandaloneLabelsNonInteractive()
        {
            var root = new VisualElement();
            var label = new Label("Informacja");
            root.Add(label);

            UiControlStates.Normalize(root);

            Assert.That(label.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [Test]
        public void SelectedClassIsSharedAcrossScreens()
        {
            var button = new Button();

            UiControlStates.SetSelected(button, true);
            Assert.That(
                button.ClassListContains(UiControlStates.SelectedClass),
                Is.True);

            UiControlStates.SetSelected(button, false);
            Assert.That(
                button.ClassListContains(UiControlStates.SelectedClass),
                Is.False);
        }

        [Test]
        public void ActiveClassIsSharedAcrossScreens()
        {
            var button = new Button();

            UiControlStates.SetActive(button, true);
            Assert.That(
                button.ClassListContains(UiControlStates.ActiveClass),
                Is.True);

            UiControlStates.SetActive(button, false);
            Assert.That(
                button.ClassListContains(UiControlStates.ActiveClass),
                Is.False);
        }
    }
}
