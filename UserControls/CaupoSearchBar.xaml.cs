
using Caupo.Helpers;
using Caupo.Views;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.UserControls
{
    public class SearchSuggestion
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public object? Item { get; set; }
    }

    public sealed class SearchSuggestionSelectedEventArgs : EventArgs
    {
        public SearchSuggestionSelectedEventArgs(SearchSuggestion suggestion)
            => Suggestion = suggestion;

        public SearchSuggestion Suggestion { get; }
    }

    public partial class CaupoSearchBar : UserControl
    {
        private INotifyCollectionChanged? _observedSuggestions;
        private bool _searchCompleted;

        public CaupoSearchBar()
        {
            InitializeComponent();

            SearchTextBox.GotFocus += SearchTextBox_GotFocus;
            SearchTextBox.PreviewKeyDown += SearchTextBox_PreviewKeyDown;

            Loaded += CaupoSearchBar_Loaded;
            Unloaded += CaupoSearchBar_Unloaded;
        }

        public event EventHandler? SearchCompleted;
        public event EventHandler<SearchSuggestionSelectedEventArgs>? SuggestionSelected;

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(CaupoSearchBar),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSearchTextChanged));

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SuggestionsProperty =
            DependencyProperty.Register(
                nameof(Suggestions),
                typeof(IEnumerable),
                typeof(CaupoSearchBar),
                new PropertyMetadata(null, OnSuggestionsChanged));

        public IEnumerable? Suggestions
        {
            get => (IEnumerable?)GetValue(SuggestionsProperty);
            set => SetValue(SuggestionsProperty, value);
        }

        public static readonly DependencyProperty AreSuggestionsVisibleProperty =
            DependencyProperty.Register(
                nameof(AreSuggestionsVisible),
                typeof(bool),
                typeof(CaupoSearchBar),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool AreSuggestionsVisible
        {
            get => (bool)GetValue(AreSuggestionsVisibleProperty);
            set => SetValue(AreSuggestionsVisibleProperty, value);
        }

        public static readonly DependencyProperty FirstCommandProperty =
            DependencyProperty.Register(
                nameof(FirstCommand),
                typeof(ICommand),
                typeof(CaupoSearchBar));

        public ICommand? FirstCommand
        {
            get => (ICommand?)GetValue(FirstCommandProperty);
            set => SetValue(FirstCommandProperty, value);
        }

        public static readonly DependencyProperty PreviousCommandProperty =
            DependencyProperty.Register(
                nameof(PreviousCommand),
                typeof(ICommand),
                typeof(CaupoSearchBar));

        public ICommand? PreviousCommand
        {
            get => (ICommand?)GetValue(PreviousCommandProperty);
            set => SetValue(PreviousCommandProperty, value);
        }

        public static readonly DependencyProperty NextCommandProperty =
            DependencyProperty.Register(
                nameof(NextCommand),
                typeof(ICommand),
                typeof(CaupoSearchBar));

        public ICommand? NextCommand
        {
            get => (ICommand?)GetValue(NextCommandProperty);
            set => SetValue(NextCommandProperty, value);
        }

        public static readonly DependencyProperty LastCommandProperty =
            DependencyProperty.Register(
                nameof(LastCommand),
                typeof(ICommand),
                typeof(CaupoSearchBar));

        public ICommand? LastCommand
        {
            get => (ICommand?)GetValue(LastCommandProperty);
            set => SetValue(LastCommandProperty, value);
        }

        private static void OnSearchTextChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var bar = (CaupoSearchBar)d;

            bar._searchCompleted = false;
            bar.UpdateSuggestionsVisibility();
        }

        private static void OnSuggestionsChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var bar = (CaupoSearchBar)d;

            if (bar._observedSuggestions != null)
            {
                bar._observedSuggestions.CollectionChanged -=
                    bar.Suggestions_CollectionChanged;
            }

            bar._observedSuggestions =
                e.NewValue as INotifyCollectionChanged;

            if (bar._observedSuggestions != null)
            {
                bar._observedSuggestions.CollectionChanged +=
                    bar.Suggestions_CollectionChanged;
            }

            bar.UpdateSuggestionsVisibility();
        }

        private void Suggestions_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            UpdateSuggestionsVisibility();
        }

        private void UpdateSuggestionsVisibility()
        {
            bool hasItems = false;

            if (Suggestions != null)
            {
                var enumerator = Suggestions.GetEnumerator();

                try
                {
                    hasItems = enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }

            AreSuggestionsVisible =
                !_searchCompleted &&
                !string.IsNullOrWhiteSpace(SearchText) &&
                hasItems;
        }

        public void CompleteSearch()
        {
            _searchCompleted = true;
            AreSuggestionsVisible = false;

            SearchCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void SearchTextBox_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            CompleteSearch();
            e.Handled = true;
        }

        private void SuggestionsList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (SuggestionsList.SelectedItem is not SearchSuggestion suggestion)
                return;

            SuggestionsList.SelectedItem = null;

            CompleteSearch();

            SuggestionSelected?.Invoke(
                this,
                new SearchSuggestionSelectedEventArgs(suggestion));

            MainWindow.Instance?.HideKeyboard();
        }

        private void CaupoSearchBar_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            VirtualKeyboardManager.EnterPressed +=
                VirtualKeyboardManager_EnterPressed;
        }

        private void CaupoSearchBar_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            VirtualKeyboardManager.EnterPressed -=
                VirtualKeyboardManager_EnterPressed;
        }

        private void VirtualKeyboardManager_EnterPressed()
        {
            if (SearchTextBox.IsKeyboardFocusWithin)
                CompleteSearch();
        }

        private void SearchTextBox_GotFocus(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                SearchTextBox.SelectAll();
                MainWindow.Instance?.ShowKeyboard();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[CaupoSearchBar] VirtualKeyboard: " + ex);
            }
        }
    }
}
