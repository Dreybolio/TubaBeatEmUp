using System;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    public enum ElementLocation
    {
        Header,
        Footer,
        Collection,
        Scene,
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ASMWindowElementAttribute : Attribute
    {

        /// <summary>Specifies the location of this element.</summary>
        public ElementLocation Location { get; }

        /// <summary>Gets if this element is visible by default.</summary>
        public bool IsVisibleByDefault { get; }

        /// <summary>A name to distinguish this from other attributes on the same method.</summary>
        public string Name { get; }

        /// <summary>Defines a new ASM window element.</summary>
        /// <param name="location">Specifies the location of this element.</param>
        /// <param name="isVisibleByDefault">Specifies whatever this element should be visible by default.</param>
        /// <param name="name">Specifies the name to use in case attribute is used mulitple times for a singular callback.</param>
        public ASMWindowElementAttribute(ElementLocation location, bool isVisibleByDefault = false, string name = null)
        {
            Location = location;
            IsVisibleByDefault = isVisibleByDefault;
            Name = name;
        }

    }

    /// <summary>Provides utility methods for working with <see cref="VisualElement"/>.</summary>
    public static class UIElementUtility
    {

        /// <summary>Applies font awesome <i>free</i> to the <see cref="VisualElement"/>.</summary>
        /// <remarks>Note that not all icons are available (no clue why, ask unity).</remarks>
        /// <param name="button">The button to apply font awesome to.</param>
        /// <param name="solid">Applies solid style. Default.</param>
        /// <param name="regular">Applies regular style.</param>
        /// <param name="brands">Applies brands style.</param>
        public static void UseFontAwesome(this VisualElement button, bool? solid = null, bool? regular = null, bool? brands = null)
        {

            var count = new[] { regular, solid, brands }.Count(b => b.HasValue);
            if (count == 0)
                solid = true;
            else if (count > 1)
                throw new ArgumentException("Only one flag may be set.");

            button.EnableInClassList("fontAwesome", solid is true);
            button.EnableInClassList("fontAwesomeRegular", regular is true);
            button.EnableInClassList("fontAwesomeBrands", brands is true);

        }

    }

}
