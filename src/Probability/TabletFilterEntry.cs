using System;
using UnityEngine;

namespace Probability
{
    /// <summary>
    /// Adds the item-category filter button to the negotiation tablet: the screen the
    /// player is looking at while a customer is buying.
    ///
    /// The tablet window is created by NegociationUIManager at scene start and reused
    /// for every customer, so instead of patching that private method we poll its
    /// public <c>Instance</c>/<c>uiObject</c> fields from <see cref="ModEntry.Tick"/>
    /// and add the button once per tablet instance. The button sits under the tablet
    /// root, so it follows the tablet's show/hide and zoom for free.
    /// </summary>
    internal static class TabletFilterEntry
    {
        private const string ButtonName = "ProbabilityFilterButton";

        // Root coordinates of the 644x818 tablet: the shell header is the strip above
        // the screen (screen starts at y=54). The built-in header buttons are Resize
        // (x=486), Bargain (x=522) and Close (x=558), all 32 px; this button sits in
        // the empty part of the bezel, left of them.
        private const float ButtonX = 378f;
        private const float ButtonY = 8f;
        private const float ButtonWidth = 88f;
        private const float ButtonHeight = 32f;

        private const int CheckIntervalFrames = 30;

        private static bool failureLogged;

        /// <summary>Called every frame by <see cref="ModEntry.Tick"/>.</summary>
        internal static void Tick()
        {
            if (Time.frameCount % CheckIntervalFrames != 0)
            {
                return;
            }
            try
            {
                NegociationUIManager manager = NegociationUIManager.Instance;
                if (manager == null || manager.uiObject == null)
                {
                    return;
                }
                Attach(manager.uiObject);
            }
            catch (Exception e)
            {
                if (!failureLogged)
                {
                    failureLogged = true;
                    ModEntry.Log.Error("TabletFilterEntry.Tick failed: " + e.Message);
                }
            }
        }

        private static void Attach(GameObject tabletRoot)
        {
            if (tabletRoot == null || tabletRoot.transform.Find(ButtonName) != null)
            {
                return;
            }
            NegotiationTabletFrame frame = tabletRoot.GetComponent<NegotiationTabletFrame>();
            if (frame == null)
            {
                return;
            }
            TabletButton button = TabletUI.SmallButton(
                frame,
                tabletRoot.transform,
                ButtonX,
                ButtonY,
                ButtonWidth,
                ButtonHeight,
                Labels.Get("ui_entry_title", "Filter"),
                FilterWindow.Toggle);
            if (button == null)
            {
                return;
            }
            button.name = ButtonName;
            TabletUI.AddTooltip(
                button.gameObject,
                () => Labels.Get("ui_entry_tip", "Open or close the item category filter"));
            ModEntry.Log.Log("filter button added to the negotiation tablet.");
        }
    }
}
