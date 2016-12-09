﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace XamForms.Controls
{
	public partial class Calendar : ContentView
	{
		List<Label> dayLabels, weekNumberLabels;
		List<CalendarButton> buttons;
		public Grid DayLabels, MainCalendar, WeekNumbers;
		StackLayout calendar;
		protected Grid details;

		public Calendar()
		{
			InitializeComponent();
			MonthNavigation.HeightRequest = Device.OS == TargetPlatform.Windows ? 50 : 32;
			TitleLabel = CenterLabel;
			TitleLeftArrow = LeftArrow;
			TitleRightArrow = RightArrow;
			MonthNavigationLayout = MonthNavigation;
			LeftArrow.Clicked += LeftArrowClickedEvent;
			RightArrow.Clicked += RightArrowClickedEvent;
			dayLabels = new List<Label>();
			weekNumberLabels = new List<Label>();
			buttons = new List<CalendarButton>();

			var columDef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
			var rowDef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
			DayLabels = new Grid { VerticalOptions = LayoutOptions.Start, RowSpacing = 0, ColumnSpacing = 0, Padding = 0 };
			DayLabels.ColumnDefinitions = new ColumnDefinitionCollection { columDef, columDef, columDef, columDef, columDef, columDef, columDef };
			MainCalendar = new Grid { VerticalOptions = LayoutOptions.Start, RowSpacing = 0, ColumnSpacing = 0, Padding = 1, BackgroundColor = BorderColor };
			MainCalendar.ColumnDefinitions = new ColumnDefinitionCollection { columDef, columDef, columDef, columDef, columDef, columDef, columDef };
			MainCalendar.RowDefinitions = new RowDefinitionCollection { rowDef, rowDef, rowDef, rowDef, rowDef, rowDef };
			WeekNumbers = new Grid { VerticalOptions = LayoutOptions.FillAndExpand, HorizontalOptions = LayoutOptions.Start, RowSpacing = 0, ColumnSpacing = 0, Padding = new Thickness(0, 0, 0, 0) };
			WeekNumbers.ColumnDefinitions = new ColumnDefinitionCollection { columDef };
			WeekNumbers.RowDefinitions = new RowDefinitionCollection { rowDef, rowDef, rowDef, rowDef, rowDef, rowDef };
			CalendarViewType = DateTypeEnum.Normal;
			/*var panGesture = new PanGestureRecognizer();panGesture.PanUpdated += (s, e) =>
			{
				var t = e;
			};
			MainCalendar.GestureRecognizers.Add(panGesture);*/
			SelectedDates = new List<DateTime>();
			DayLabels.PropertyChanged += (sender, e) =>
			{
				if (DayLabels.Height > 0) WeekNumbers.Padding = new Thickness(0, DayLabels.Height, 0, 0);
			};
			YearsRow = 4;
			YearsColumn = 4;
		}

		protected async override void OnParentSet()
		{
			if (Device.OS == TargetPlatform.Windows || Device.OS == TargetPlatform.WinPhone)
			{
				FillCalendarWindows();
			}
			else {
				// iOS and Android can create controls on another thread when they are not attached to the main ui yet, 
				// windows can not
				await FillCalendar();
			}
			calendar = new StackLayout { Padding = 0, Spacing = 0, Orientation = StackOrientation.Vertical, Children = { DayLabels, MainCalendar } };
			ShowHideWeekNumbers();
			base.OnParentSet();
		}

		protected void ShowHideWeekNumbers()
		{
			if (calendar == null) return;
			MainView.Children.Remove(calendar);
			if (ShowNumberOfWeek)
			{
				calendar = new StackLayout { Padding = 0, Spacing = 0, Orientation = StackOrientation.Horizontal, Children = { WeekNumbers, calendar } };
			}
			else
			{
				calendar = new StackLayout { Padding = 0, Spacing = 0, Orientation = StackOrientation.Vertical, Children = { DayLabels, MainCalendar } };
			}
			MainView.Children.Add(calendar);
		}

		protected void FillCalendarWindows()
		{
			for (int r = 0; r < 6; r++)
			{
				for (int c = 0; c < 7; c++)
				{
					if (r == 0)
					{
						dayLabels.Add(new Label
						{
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center,
							TextColor = WeekdaysTextColor,
							FontSize = WeekdaysFontSize,
							FontAttributes = WeekdaysFontAttributes
						});
						DayLabels.Children.Add(dayLabels.Last(), c, r);
					}
					buttons.Add(new CalendarButton
					{
						BorderRadius = 0,
						BorderWidth = BorderWidth,
						BorderColor = BorderColor,
						FontSize = DatesFontSize,
						BackgroundColor = DatesBackgroundColor,
						HorizontalOptions = LayoutOptions.FillAndExpand,
						VerticalOptions = LayoutOptions.FillAndExpand
					});
					buttons.Last().Clicked += DateClickedEvent;
					MainCalendar.Children.Add(buttons.Last(), c, r);
				}
				weekNumberLabels.Add(new Label
				{
					HorizontalOptions = LayoutOptions.FillAndExpand,
					VerticalOptions = LayoutOptions.FillAndExpand,
					TextColor = NumberOfWeekTextColor,
					BackgroundColor = NumberOfWeekBackgroundColor,
					VerticalTextAlignment = TextAlignment.Center,
					HorizontalTextAlignment = TextAlignment.Center,
					FontSize = NumberOfWeekFontSize,
					FontAttributes = WeekdaysFontAttributes
				});
				WeekNumbers.Children.Add(weekNumberLabels.Last(), 0, r);
			}
			WeekNumbers.WidthRequest = NumberOfWeekFontSize + (NumberOfWeekFontSize / 2) + 6;
			ChangeCalendar(CalandarChanges.All);
		}

		protected Task FillCalendar()
		{
			return Task.Factory.StartNew(() =>
			{
				FillCalendarWindows();
			});
		}

		public static readonly BindableProperty DisableAllDatesProperty = BindableProperty.Create(nameof(DisableAllDates), typeof(bool), typeof(Calendar), false,
				propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar)?.RaiseSpecialDatesChanged());

		/// <summary>
		/// Gets or sets wether all dates should be disabled by default or not
		/// </summary>
		/// <value></value>
		public bool DisableAllDates
		{
			get { return (bool)GetValue(DisableAllDatesProperty); }
			set { SetValue(DisableAllDatesProperty, value); }
		}


		public static readonly BindableProperty SelectedDateProperty =
			BindableProperty.Create(nameof(SelectedDate), typeof(DateTime?), typeof(Calendar), null, BindingMode.TwoWay,
				propertyChanged: (bindable, oldValue, newValue) =>
				{
					if ((bindable as Calendar).ChangeSelectedDate(newValue as DateTime?))
					{
						(bindable as Calendar).SelectedDate = null;
					}
				});

		/// <summary>
		/// Gets or sets a date the selected date
		/// </summary>
		/// <value>The selected date.</value>
		public DateTime? SelectedDate
		{
			get { return (DateTime?)GetValue(SelectedDateProperty); }
			set { SetValue(SelectedDateProperty, value.HasValue ? value.Value.Date : value); }
		}

		public static readonly BindableProperty MultiSelectDatesProperty = BindableProperty.Create(nameof(MultiSelectDates), typeof(bool), typeof(Calendar), false);

		/// <summary>
		/// Gets or sets multiple Dates can be selected.
		/// </summary>
		public bool MultiSelectDates
		{
			get { return (bool)GetValue(MultiSelectDatesProperty); }
			set { SetValue(MultiSelectDatesProperty, value); }
		}


		public static readonly BindableProperty SelectedDatesProperty = BindableProperty.Create(nameof(SelectedDates), typeof(List<DateTime>), typeof(Calendar), null);
		/// <summary>
		/// Gets the selected dates when MultiSelectDates is true
		/// </summary>
		/// <value>The selected date.</value>
		public List<DateTime> SelectedDates
		{
			get { return (List<DateTime>)GetValue(SelectedDatesProperty); }
			protected set { SetValue(SelectedDatesProperty, value); }
		}

		public static readonly BindableProperty MinDateProperty =
			BindableProperty.Create(nameof(MinDate), typeof(DateTime?), typeof(Calendar), null,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeCalendar(CalandarChanges.MaxMin));

		/// <summary>
		/// Gets or sets the minimum date.
		/// </summary>
		/// <value>The minimum date.</value>
		public DateTime? MinDate
		{
			get { return (DateTime?)GetValue(MinDateProperty); }
			set { SetValue(MinDateProperty, value); ChangeCalendar(CalandarChanges.MaxMin); }
		}

		public static readonly BindableProperty MaxDateProperty =
			BindableProperty.Create(nameof(MaxDate), typeof(DateTime?), typeof(Calendar), null,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeCalendar(CalandarChanges.MaxMin));

		/// <summary>
		/// Gets or sets the max date.
		/// </summary>
		/// <value>The max date.</value>
		public DateTime? MaxDate
		{
			get { return (DateTime?)GetValue(MaxDateProperty); }
			set { SetValue(MaxDateProperty, value); }
		}

		public static readonly BindableProperty StartDateProperty =
			BindableProperty.Create(nameof(StartDate), typeof(DateTime), typeof(Calendar), DateTime.Now,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeCalendar(CalandarChanges.StartDate));

		/// <summary>
		/// Gets or sets a date, to pick the month, the calendar is focused on
		/// </summary>
		/// <value>The start date.</value>
		public DateTime StartDate
		{
			get { return (DateTime)GetValue(StartDateProperty); }
			set { SetValue(StartDateProperty, value); }
		}

		public static readonly BindableProperty StartDayProperty =
			BindableProperty.Create(nameof(StartDate), typeof(DayOfWeek), typeof(Calendar), DayOfWeek.Sunday,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeCalendar(CalandarChanges.StartDay));

		/// <summary>
		/// Gets or sets the day the calendar starts the week with.
		/// </summary>
		/// <value>The start day.</value>
		public DayOfWeek StartDay
		{
			get { return (DayOfWeek)GetValue(StartDayProperty); }
			set { SetValue(StartDayProperty, value); }
		}

		public static readonly BindableProperty BorderWidthProperty =
			BindableProperty.Create(nameof(BorderWidth), typeof(int), typeof(Calendar), Device.OS == TargetPlatform.iOS ? 1 : 3,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeBorderWidth((int)newValue, (int)oldValue));

		protected void ChangeBorderWidth(int newValue, int oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => !b.IsSelected && b.IsEnabled).ForEach(b => b.BorderWidth = newValue);
		}

		/// <summary>
		/// Gets or sets the border width of the calendar.
		/// </summary>
		/// <value>The width of the border.</value>
		public int BorderWidth
		{
			get { return (int)GetValue(BorderWidthProperty); }
			set { SetValue(BorderWidthProperty, value); }
		}

		public static readonly BindableProperty OuterBorderWidthProperty =
			BindableProperty.Create(nameof(OuterBorderWidth), typeof(int), typeof(Calendar), Device.OS == TargetPlatform.iOS ? 1 : 3,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).MainCalendar.Padding = (int)newValue);

		/// <summary>
		/// Gets or sets the width of the whole calandar border.
		/// </summary>
		/// <value>The width of the outer border.</value>
		public int OuterBorderWidth
		{
			get { return (int)GetValue(OuterBorderWidthProperty); }
			set { SetValue(OuterBorderWidthProperty, value); }
		}

		public static readonly BindableProperty BorderColorProperty =
			BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(Calendar), Color.FromHex("#dddddd"),
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeBorderColor((Color)newValue, (Color)oldValue));

		protected void ChangeBorderColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			MainCalendar.BackgroundColor = newValue;
			buttons.FindAll(b => b.IsEnabled && !b.IsSelected).ForEach(b => b.BorderColor = newValue);
		}

		/// <summary>
		/// Gets or sets the border color of the calendar.
		/// </summary>
		/// <value>The color of the border.</value>
		public Color BorderColor
		{
			get { return (Color)GetValue(BorderColorProperty); }
			set { SetValue(BorderColorProperty, value); }
		}

		public static readonly BindableProperty DatesBackgroundColorProperty =
			BindableProperty.Create(nameof(DatesBackgroundColor), typeof(Color), typeof(Calendar), Color.White,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesBackgroundColor((Color)newValue, (Color)oldValue));

		protected void ChangeDatesBackgroundColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsEnabled && (!b.IsSelected || !SelectedBackgroundColor.HasValue)).ForEach(b => b.BackgroundColor = newValue);
		}

		/// <summary>
		/// Gets or sets the background color of the normal dates.
		/// </summary>
		/// <value>The color of the dates background.</value>
		public Color DatesBackgroundColor
		{
			get { return (Color)GetValue(DatesBackgroundColorProperty); }
			set { SetValue(DatesBackgroundColorProperty, value); }
		}

		public static readonly BindableProperty DatesTextColorProperty =
			BindableProperty.Create(nameof(DatesTextColor), typeof(Color), typeof(Calendar), Color.Black,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesTextColor((Color)newValue, (Color)oldValue));

		protected void ChangeDatesTextColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsEnabled && (!b.IsSelected || !SelectedTextColor.HasValue) && !b.IsOutOfMonth).ForEach(b => b.TextColor = newValue);
		}

		/// <summary>
		/// Gets or sets the text color of the normal dates.
		/// </summary>
		/// <value>The color of the dates text.</value>
		public Color DatesTextColor
		{
			get { return (Color)GetValue(DatesTextColorProperty); }
			set { SetValue(DatesTextColorProperty, value); }
		}

		public static readonly BindableProperty DatesFontAttributesProperty =
			BindableProperty.Create(nameof(DatesFontAttributes), typeof(FontAttributes), typeof(Calendar), FontAttributes.None,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesFontAttributes((FontAttributes)newValue, (FontAttributes)oldValue));

		protected void ChangeDatesFontAttributes(FontAttributes newValue, FontAttributes oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsEnabled && (!b.IsSelected || !SelectedTextColor.HasValue) && !b.IsOutOfMonth).ForEach(b => b.FontAttributes = newValue);
		}

		/// <summary>
		/// Gets or sets the dates font attributes.
		/// </summary>
		/// <value>The dates font attributes.</value>
		public FontAttributes DatesFontAttributes
		{
			get { return (FontAttributes)GetValue(DatesFontAttributesProperty); }
			set { SetValue(DatesFontAttributesProperty, value); }
		}

		public static readonly BindableProperty DatesTextColorOutsideMonthProperty =
			BindableProperty.Create(nameof(DatesTextColorOutsideMonth), typeof(Color), typeof(Calendar), Color.FromHex("#aaaaaa"),
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesTextColorOutsideMonth((Color)newValue, (Color)oldValue));

		protected void ChangeDatesTextColorOutsideMonth(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsEnabled && !b.IsSelected && b.IsOutOfMonth).ForEach(b => b.TextColor = newValue);
		}

		/// <summary>
		/// Gets or sets the text color of the dates not in the focused month.
		/// </summary>
		/// <value>The dates text color outside month.</value>
		public Color DatesTextColorOutsideMonth
		{
			get { return (Color)GetValue(DatesTextColorOutsideMonthProperty); }
			set { SetValue(DatesTextColorOutsideMonthProperty, value); }
		}

		public static readonly BindableProperty DatesBackgroundColorOutsideMonthProperty =
			BindableProperty.Create(nameof(DatesBackgroundColorOutsideMonth), typeof(Color), typeof(Calendar), Color.White,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesBackgroundColorOutsideMonth((Color)newValue, (Color)oldValue));

		protected void ChangeDatesBackgroundColorOutsideMonth(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsEnabled && !b.IsSelected && b.IsOutOfMonth).ForEach(b => b.BackgroundColor = newValue);
		}

		/// <summary>
		/// Gets or sets the background color of the dates not in the focused month.
		/// </summary>
		/// <value>The dates background color outside month.</value>
		public Color DatesBackgroundColorOutsideMonth
		{
			get { return (Color)GetValue(DatesBackgroundColorOutsideMonthProperty); }
			set { SetValue(DatesBackgroundColorOutsideMonthProperty, value); }
		}

		public static readonly BindableProperty DatesFontAttributesOutsideMonthProperty =
			BindableProperty.Create(nameof(DatesFontAttributesOutsideMonth), typeof(FontAttributes), typeof(Calendar), FontAttributes.None,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesFontAttributesOutsideMonth((FontAttributes)newValue, (FontAttributes)oldValue));

		protected void ChangeDatesFontAttributesOutsideMonth(FontAttributes newValue, FontAttributes oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsEnabled && !b.IsSelected && b.IsOutOfMonth).ForEach(b => b.FontAttributes = newValue);
		}

		/// <summary>
		/// Gets or sets the dates font attributes for dates outside of the month.
		/// </summary>
		/// <value>The dates font attributes.</value>
		public FontAttributes DatesFontAttributesOutsideMonth
		{
			get { return (FontAttributes)GetValue(DatesFontAttributesOutsideMonthProperty); }
			set { SetValue(DatesFontAttributesOutsideMonthProperty, value); }
		}

		public static readonly BindableProperty DatesFontSizeProperty =
			BindableProperty.Create(nameof(DatesFontSize), typeof(double), typeof(Calendar), 20.0,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDatesFontSize((double)newValue, (double)oldValue));

		protected void ChangeDatesFontSize(double newValue, double oldValue)
		{
			if (Math.Abs(newValue - oldValue) < 0.01) return;
			buttons.FindAll(b => !b.IsSelected && b.IsEnabled).ForEach(b => b.FontSize = newValue);
		}

		/// <summary>
		/// Gets or sets the font size of the normal dates.
		/// </summary>
		/// <value>The size of the dates font.</value>
		public double DatesFontSize
		{
			get { return (double)GetValue(DatesFontSizeProperty); }
			set { SetValue(DatesFontSizeProperty, value); }
		}

		public static readonly BindableProperty WeekdaysTextColorProperty =
			BindableProperty.Create(nameof(WeekdaysTextColor), typeof(Color), typeof(Calendar), Color.FromHex("#aaaaaa"),
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeWeekdaysTextColor((Color)newValue, (Color)oldValue));

		protected void ChangeWeekdaysTextColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			dayLabels.ForEach(l => l.TextColor = newValue);
		}

		/// <summary>
		/// Gets or sets the text color of the weekdays labels.
		/// </summary>
		/// <value>The color of the weekdays text.</value>
		public Color WeekdaysTextColor
		{
			get { return (Color)GetValue(WeekdaysTextColorProperty); }
			set { SetValue(WeekdaysTextColorProperty, value); }
		}

		public static readonly BindableProperty WeekdaysBackgroundColorProperty =
			BindableProperty.Create(nameof(WeekdaysBackgroundColor), typeof(Color), typeof(Calendar), Color.Transparent,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeWeekdaysBackgroundColor((Color)newValue, (Color)oldValue));

		protected void ChangeWeekdaysBackgroundColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			dayLabels.ForEach(l => l.BackgroundColor = newValue);
		}

		/// <summary>
		/// Gets or sets the background color of the weekdays labels.
		/// </summary>
		/// <value>The color of the weekdays background.</value>
		public Color WeekdaysBackgroundColor
		{
			get { return (Color)GetValue(WeekdaysBackgroundColorProperty); }
			set { SetValue(WeekdaysBackgroundColorProperty, value); }
		}

		public static readonly BindableProperty WeekdaysFontSizeProperty =
			BindableProperty.Create(nameof(WeekdaysFontSize), typeof(double), typeof(Calendar), 18.0,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeWeekdaysFontSize((double)newValue, (double)oldValue));

		protected void ChangeWeekdaysFontSize(double newValue, double oldValue)
		{
			if (Math.Abs(newValue - oldValue) < 0.01) return;
			dayLabels.ForEach(l => l.FontSize = newValue);
		}

		/// <summary>
		/// Gets or sets the font size of the weekday labels.
		/// </summary>
		/// <value>The size of the weekdays font.</value>
		public double WeekdaysFontSize
		{
			get { return (double)GetValue(WeekdaysFontSizeProperty); }
			set { SetValue(WeekdaysFontSizeProperty, value); }
		}

		public static readonly BindableProperty WeekdaysFormatProperty =
			BindableProperty.Create(nameof(WeekdaysFormat), typeof(string), typeof(Calendar), "ddd",
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeWeekdays());

		public static readonly BindableProperty WeekdaysFontAttributesProperty =
			BindableProperty.Create(nameof(WeekdaysFontAttributes), typeof(FontAttributes), typeof(Calendar), FontAttributes.None,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeWeekdaysFontAttributes((FontAttributes)newValue, (FontAttributes)oldValue));

		protected void ChangeWeekdaysFontAttributes(FontAttributes newValue, FontAttributes oldValue)
		{
			if (newValue == oldValue) return;
			weekNumberLabels.ForEach(l => l.FontAttributes = newValue);
		}

		/// <summary>
		/// Gets or sets the font attributes of the weekday labels.
		/// </summary>
		public FontAttributes WeekdaysFontAttributes
		{
			get { return (FontAttributes)GetValue(WeekdaysFontAttributesProperty); }
			set { SetValue(WeekdaysFontAttributesProperty, value); }
		}

		/// <summary>
		/// Gets or sets the date format of the weekday labels.
		/// </summary>
		/// <value>The weekdays format.</value>
		public string WeekdaysFormat
		{
			get { return (string)GetValue(WeekdaysFormatProperty); }
			set { SetValue(WeekdaysFormatProperty, value); }
		}

		public static readonly BindableProperty WeekdaysShowProperty =
			BindableProperty.Create(nameof(WeekdaysShow), typeof(bool), typeof(Calendar), true,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).DayLabels.IsVisible = (bool)newValue);

		/// <summary>
		/// Gets or sets wether to show the weekday labels.
		/// </summary>
		/// <value>The weekdays show.</value>
		public bool WeekdaysShow
		{
			get { return (bool)GetValue(WeekdaysShowProperty); }
			set { SetValue(WeekdaysShowProperty, value); }
		}

		public static readonly BindableProperty EnableTitleMonthYearDetailsProperty =
		BindableProperty.Create(nameof(EnableTitleMonthYearDetails), typeof(bool), typeof(Calendar), false,
			propertyChanged: (bindable, oldValue, newValue) =>
			{
				(bindable as Calendar).TitleLabel.GestureRecognizers.Clear();
				if (!(bool)newValue) return;
				var gr = new TapGestureRecognizer();
				gr.Tapped += (sender, e) => (bindable as Calendar).NextMonthYearDetails();
				(bindable as Calendar).TitleLabel.GestureRecognizers.Add(gr);
			});

		/// <summary>
		/// Gets or sets wether on Title pressed the month, year or normal view is showen
		/// </summary>
		/// <value>The weekdays show.</value>
		public bool EnableTitleMonthYearDetails
		{
			get { return (bool)GetValue(EnableTitleMonthYearDetailsProperty); }
			set { SetValue(EnableTitleMonthYearDetailsProperty, value); }
		}

		public DateTypeEnum CalendarViewType { get; protected set; }

		public void PrevMonthYearDetails()
		{
			switch (CalendarViewType)
			{
				case DateTypeEnum.Normal: ShowYears(); break;
				case DateTypeEnum.Month: ShowNormal(); break;
				case DateTypeEnum.Year: ShowMonths(); break;
				default: ShowNormal(); break;
			}
		}

		public void NextMonthYearDetails()
		{
			switch (CalendarViewType)
			{
				case DateTypeEnum.Normal: ShowMonths(); break;
				case DateTypeEnum.Month: ShowYears(); break;
				case DateTypeEnum.Year: ShowNormal(); break;
				default: ShowNormal(); break;
			}
		}

		public void ShowNormal()
		{
			if(details != null) MainView.Children.Remove(details);
			if(!MainView.Children.Contains(calendar)) MainView.Children.Add(calendar);
			CalendarViewType = DateTypeEnum.Normal;
			TitleLeftArrow.IsVisible = true;
			TitleRightArrow.IsVisible = true;
		}

		public void ShowMonths()
		{
			if (MainView.Children.Contains(calendar)) MainView.Children.Remove(calendar);
			if (details != null && MainView.Children.Contains(details)) MainView.Children.Remove(details);
			var columDef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
			var rowDef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
			details = new Grid { VerticalOptions = LayoutOptions.CenterAndExpand, RowSpacing = 0, ColumnSpacing = 0, Padding = 1, BackgroundColor = BorderColor };
			details.ColumnDefinitions = new ColumnDefinitionCollection { columDef, columDef, columDef };
			details.RowDefinitions = new RowDefinitionCollection { rowDef, rowDef, rowDef, rowDef };
			for (int r = 0; r < 4; r++)
			{
				for (int c = 0; c < 3; c++)
				{
					var b = new CalendarButton
					{
						HorizontalOptions = LayoutOptions.CenterAndExpand,
						VerticalOptions = LayoutOptions.CenterAndExpand,
						Text = DateTimeFormatInfo.CurrentInfo.MonthNames[(r * 3) + c],
						Date = new DateTime(StartDate.Year, (r * 3) + c + 1, 1).Date,
						BackgroundColor = DatesBackgroundColor,
						TextColor = DatesTextColor,
						FontSize = DatesFontSize,
						FontAttributes = DatesFontAttributes,
						WidthRequest = calendar.Width / 3 - BorderWidth,
						HeightRequest = calendar.Height / 4 - BorderWidth
					};
					b.Clicked += (sender, e) => {
						StartDate = (sender as CalendarButton).Date.Value;
						PrevMonthYearDetails();
					};
					details.Children.Add(b, c, r);
				}
			}
			details.WidthRequest = calendar.Width;
			details.HeightRequest = calendar.Height;
			MainView.Children.Add(details);
			CalendarViewType = DateTypeEnum.Month;
			TitleLeftArrow.IsVisible = false;
			TitleRightArrow.IsVisible = false;
		}

		public int YearsRow { get; set; }
		public int YearsColumn { get; set; }

		public void ShowYears()
		{
			if (MainView.Children.Contains(calendar)) MainView.Children.Remove(calendar);
			if (details != null && MainView.Children.Contains(details)) MainView.Children.Remove(details);
			var columDef = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
			var rowDef = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
			details = new Grid { VerticalOptions = LayoutOptions.CenterAndExpand, RowSpacing = 0, ColumnSpacing = 0, Padding = 1, BackgroundColor = BorderColor };
			details.ColumnDefinitions = new ColumnDefinitionCollection { columDef, columDef, columDef, columDef };
			details.RowDefinitions = new RowDefinitionCollection { rowDef, rowDef, rowDef, rowDef };
			for (int r = 0; r < YearsRow; r++)
			{
				for (int c = 0; c < YearsColumn; c++)
				{
					var t =  (r * YearsColumn) + c + 1;
					var b = new CalendarButton
					{
						HorizontalOptions = LayoutOptions.CenterAndExpand,
						VerticalOptions = LayoutOptions.CenterAndExpand,
						Text = string.Format("{0}", StartDate.Year + (t - (YearsColumn * YearsRow / 2))),
						Date = new DateTime(StartDate.Year + (t - (YearsColumn*YearsRow/2)), StartDate.Month, 1).Date,
						BackgroundColor = DatesBackgroundColor,
						TextColor = DatesTextColor,
						FontSize = DatesFontSize,
						FontAttributes = DatesFontAttributes,
						WidthRequest = (calendar.Width / YearsRow) - BorderWidth,
						HeightRequest = calendar.Height / YearsColumn - BorderWidth
					};
					b.Clicked += (sender, e) =>
					{
						StartDate = (sender as CalendarButton).Date.Value;
						PrevMonthYearDetails();
					};
					details.Children.Add(b, c, r);
				}
			}
			details.WidthRequest = calendar.Width;
			details.HeightRequest = calendar.Height;
			MainView.Children.Add(details);
			CalendarViewType = DateTypeEnum.Year;
			TitleLeftArrow.IsVisible = true;
			TitleRightArrow.IsVisible = true;
		}

		public static readonly BindableProperty NumberOfWeekTextColorProperty =
		  BindableProperty.Create(nameof(NumberOfWeekTextColor), typeof(Color), typeof(Calendar), Color.FromHex("#aaaaaa"),
								  propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeNumberOfWeekTextColor((Color)newValue, (Color)oldValue));

		protected void ChangeNumberOfWeekTextColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			weekNumberLabels.ForEach(l => l.TextColor = newValue);
		}

		/// <summary>
		/// Gets or sets the text color of the number of the week labels.
		/// </summary>
		/// <value>The color of the weekdays text.</value>
		public Color NumberOfWeekTextColor
		{
			get { return (Color)GetValue(NumberOfWeekTextColorProperty); }
			set { SetValue(NumberOfWeekTextColorProperty, value); }
		}

		public static readonly BindableProperty NumberOfWeekBackgroundColorProperty =
			BindableProperty.Create(nameof(NumberOfWeekBackgroundColor), typeof(Color), typeof(Calendar), Color.Transparent,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeNumberOfWeekBackgroundColor((Color)newValue, (Color)oldValue));

		protected void ChangeNumberOfWeekBackgroundColor(Color newValue, Color oldValue)
		{
			if (newValue == oldValue) return;
			weekNumberLabels.ForEach(l => l.BackgroundColor = newValue);
		}

		/// <summary>
		/// Gets or sets the background color of the number of the week labels.
		/// </summary>
		/// <value>The color of the number of the weeks background.</value>
		public Color NumberOfWeekBackgroundColor
		{
			get { return (Color)GetValue(NumberOfWeekBackgroundColorProperty); }
			set { SetValue(NumberOfWeekBackgroundColorProperty, value); }
		}

		public static readonly BindableProperty NumberOfWeekFontSizeProperty =
			BindableProperty.Create(nameof(NumberOfWeekFontSize), typeof(double), typeof(Calendar), 14.0,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeNumberOfWeekFontSize((double)newValue, (double)oldValue));

		protected void ChangeNumberOfWeekFontSize(double newValue, double oldValue)
		{
			if (Math.Abs(newValue - oldValue) < 0.01) return;
			WeekNumbers.WidthRequest = newValue + (newValue/2) + 6;
			weekNumberLabels.ForEach(l => l.FontSize = newValue);
		}

		/// <summary>
		/// Gets or sets the font size of the number of the week labels.
		/// </summary>
		/// <value>The size of the weekdays font.</value>
		public double NumberOfWeekFontSize
		{
			get { return (double)GetValue(NumberOfWeekFontSizeProperty); }
			set { SetValue(NumberOfWeekFontSizeProperty, value); }
		}

		public static readonly BindableProperty NumberOfWeekFontAttributesProperty =
			BindableProperty.Create(nameof(NumberOfWeekFontAttributes), typeof(FontAttributes), typeof(Calendar), FontAttributes.None,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeNumberOfWeekFontAttributes((FontAttributes)newValue, (FontAttributes)oldValue));

		protected void ChangeNumberOfWeekFontAttributes(FontAttributes newValue, FontAttributes oldValue)
		{
			if (newValue == oldValue) return;
			weekNumberLabels.ForEach(l => l.FontAttributes = newValue);
		}

		/// <summary>
		/// Gets or sets the font attributes of the number of the week labels.
		/// </summary>
		public FontAttributes NumberOfWeekFontAttributes
		{
			get { return (FontAttributes)GetValue(NumberOfWeekFontAttributesProperty); }
			set { SetValue(NumberOfWeekFontAttributesProperty, value); }
		}

		public static readonly BindableProperty ShowNumberOfWeekProperty =
			BindableProperty.Create(nameof(ShowNumberOfWeek), typeof(bool), typeof(Calendar), false,
			                        propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ShowHideWeekNumbers());

		/// <summary>
		/// Gets or sets wether to show the number of the week labels.
		/// </summary>
		/// <value>The weekdays show.</value>
		public bool ShowNumberOfWeek
		{
			get { return (bool)GetValue(ShowNumberOfWeekProperty); }
			set { SetValue(ShowNumberOfWeekProperty, value); }
		}

        public static readonly BindableProperty MonthNavigationShowProperty =
            BindableProperty.Create(nameof(MonthNavigationShow), typeof(bool), typeof(Calendar), true,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).MonthNavigation.IsVisible = (bool)newValue);

        /// <summary>
        /// Gets or sets wether to show the month navigation.
        /// </summary>
        /// <value>The month navigation show.</value>
        public bool MonthNavigationShow
        {
            get { return (bool)GetValue(MonthNavigationShowProperty); }
            set { SetValue(MonthNavigationShowProperty, value); }
        }

        public static readonly BindableProperty TitleLabelFormatProperty =
            BindableProperty.Create(nameof(TitleLabelFormat), typeof(string), typeof(Calendar), "MMM yyyy",
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).CenterLabel.Text = ((bindable as Calendar).StartDate).ToString((string)newValue));

        /// <summary>
        /// Gets or sets the format of the title in the month navigation.
        /// </summary>
        /// <value>The title label format.</value>
        public string TitleLabelFormat
        {
            get { return (string)GetValue(TitleLabelFormatProperty); }
            set { SetValue(TitleLabelFormatProperty, value); }
        }

        /// <summary>
        /// Gets the title label in the month navigation.
        /// </summary>
        public Label TitleLabel { get; protected set; }

        /// <summary>
        /// Gets the left button of the month navigation.
        /// </summary>
        public CalendarButton TitleLeftArrow { get; protected set; }

        /// <summary>
        /// Gets the right button of the month navigation.
        /// </summary>
        public CalendarButton TitleRightArrow { get; protected set; }
		
        /// <summary>
        /// Gets the right button of the month navigation.
        /// </summary>
        public StackLayout MonthNavigationLayout { get; protected set; }
		
        public static readonly BindableProperty SelectedBorderWidthProperty =
            BindableProperty.Create(nameof(SelectedBorderWidth), typeof(int), typeof(Calendar), Device.OS == TargetPlatform.iOS ? 3 : 5,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeSelectedBorderWidth((int)newValue, (int)oldValue));

        protected void ChangeSelectedBorderWidth(int newValue, int oldValue)
        {
            if (newValue == oldValue) return;
            buttons.FindAll(b => b.IsSelected).ForEach(b => b.BorderWidth = newValue);
        }

        /// <summary>
        /// Gets or sets the border width of the selected date.
        /// </summary>
        /// <value>The width of the selected border.</value>
        public int SelectedBorderWidth
        {
            get { return (int)GetValue(SelectedBorderWidthProperty); }
            set { SetValue(SelectedBorderWidthProperty, value); }
        }

        public static readonly BindableProperty SelectedBorderColorProperty =
            BindableProperty.Create(nameof(SelectedBorderColor), typeof(Color), typeof(Calendar), Color.FromHex("#c82727"),
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeSelectedBorderColor((Color)newValue, (Color)oldValue));

        protected void ChangeSelectedBorderColor(Color newValue, Color oldValue)
        {
            if (newValue == oldValue) return;
            buttons.FindAll(b => b.IsSelected).ForEach(b => b.BorderColor = newValue);
        }

        /// <summary>
        /// Gets or sets the color of the selected date.
        /// </summary>
        /// <value>The color of the selected border.</value>
        public Color SelectedBorderColor
        {
            get { return (Color)GetValue(SelectedBorderColorProperty); }
            set { SetValue(SelectedBorderColorProperty, value); }
        }

        public static readonly BindableProperty SelectedBackgroundColorProperty =
            BindableProperty.Create(nameof(SelectedBackgroundColor), typeof(Color?), typeof(Calendar), null,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeSelectedBackgroundColor((Color?)newValue, (Color?)oldValue));

        protected void ChangeSelectedBackgroundColor(Color? newValue, Color? oldValue)
        {
            if (newValue == oldValue) return;
            if (newValue.HasValue) buttons.FindAll(b => b.IsSelected).ForEach(b => b.BackgroundColor = newValue.Value);
        }

        /// <summary>
        /// Gets or sets the background color of the selected date.
        /// </summary>
        /// <value>The color of the selected background.</value>
        public Color? SelectedBackgroundColor
        {
            get { return (Color?)GetValue(SelectedBackgroundColorProperty); }
            set { SetValue(SelectedBackgroundColorProperty, value); }
        }

        public static readonly BindableProperty SelectedTextColorProperty =
            BindableProperty.Create(nameof(SelectedTextColor), typeof(Color?), typeof(Calendar), null,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeSelectedTextColor((Color?)newValue, (Color?)oldValue));

        protected void ChangeSelectedTextColor(Color? newValue, Color? oldValue)
        {
            if (newValue == oldValue) return;
            if (newValue.HasValue) buttons.FindAll(b => b.IsSelected).ForEach(b => b.TextColor = newValue.Value);
        }

        /// <summary>
        /// Gets or sets the text color of the selected date.
        /// </summary>
        /// <value>The color of the selected text.</value>
        public Color? SelectedTextColor
        {
            get { return (Color?)GetValue(SelectedTextColorProperty); }
            set { SetValue(SelectedTextColorProperty, value); }
        }

        public static readonly BindableProperty SelectedFontSizeProperty =
            BindableProperty.Create(nameof(SelectedFontSize), typeof(double), typeof(Calendar), 20.0,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeSelectedFontSize((double)newValue, (double)oldValue));

        protected void ChangeSelectedFontSize(double newValue, double oldValue)
        {
            if (Math.Abs(newValue - oldValue) < 0.01) return;
            buttons.FindAll(b => b.IsSelected).ForEach(b => b.FontSize = newValue);
        }

		public static readonly BindableProperty SelectedFontAttributesProperty =
			BindableProperty.Create(nameof(SelectedFontAttributes), typeof(FontAttributes), typeof(Calendar), FontAttributes.None,
									propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeSelectedFontAttributes((FontAttributes)newValue, (FontAttributes)oldValue));

		protected void ChangeSelectedFontAttributes(FontAttributes newValue, FontAttributes oldValue)
		{
			if (newValue == oldValue) return;
			buttons.FindAll(b => b.IsSelected).ForEach(b => b.FontAttributes = newValue);
		}

		/// <summary>
		/// Gets or sets the dates font attributes for selected dates.
		/// </summary>
		/// <value>The dates font attributes.</value>
		public FontAttributes SelectedFontAttributes
		{
			get { return (FontAttributes)GetValue(SelectedFontAttributesProperty); }
			set { SetValue(SelectedFontAttributesProperty, value); }
		}

        /// <summary>
        /// Gets or sets the font size of the selected date.
        /// </summary>
        /// <value>The size of the selected font.</value>
        public double SelectedFontSize
        {
            get { return (double)GetValue(SelectedFontSizeProperty); }
            set { SetValue(SelectedFontSizeProperty, value); }
        }

        public static readonly BindableProperty DisabledBorderWidthProperty =
            BindableProperty.Create(nameof(DisabledBorderWidth), typeof(int), typeof(Calendar), Device.OS == TargetPlatform.iOS ? 1 : 3,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDisabledBorderWidth((int)newValue, (int)oldValue));

        protected void ChangeDisabledBorderWidth(int newValue, int oldValue)
        {
            if (newValue == oldValue) return;
            buttons.FindAll(b => !b.IsEnabled).ForEach(b => b.BorderWidth = newValue);
        }

        /// <summary>
        /// Gets or sets the border width of the disabled dates.
        /// </summary>
        /// <value>The width of the disabled border.</value>
        public int DisabledBorderWidth
        {
            get { return (int)GetValue(DisabledBorderWidthProperty); }
            set { SetValue(DisabledBorderWidthProperty, value); }
        }

        public static readonly BindableProperty DisabledBorderColorProperty =
            BindableProperty.Create(nameof(DisabledBorderColor), typeof(Color), typeof(Calendar), Color.FromHex("#cccccc"),
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDisabledBorderColor((Color)newValue, (Color)oldValue));

        protected void ChangeDisabledBorderColor(Color newValue, Color oldValue)
        {
            if (newValue == oldValue) return;
            buttons.FindAll(b => !b.IsEnabled).ForEach(b => b.BorderColor = newValue);
        }

        /// <summary>
        /// Gets or sets the border color of the disabled dates.
        /// </summary>
        /// <value>The color of the disabled border.</value>
        public Color DisabledBorderColor
        {
            get { return (Color)GetValue(DisabledBorderColorProperty); }
            set { SetValue(DisabledBorderColorProperty, value); }
        }

        public static readonly BindableProperty DisabledBackgroundColorProperty =
            BindableProperty.Create(nameof(DisabledBackgroundColor), typeof(Color), typeof(Calendar), Color.Gray,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDisabledBackgroundColor((Color)newValue, (Color)oldValue));

        protected void ChangeDisabledBackgroundColor(Color newValue, Color oldValue)
        {
            if (newValue == oldValue) return;
            buttons.FindAll(b => !b.IsEnabled).ForEach(b => b.BackgroundColor = newValue);
        }

        /// <summary>
        /// Gets or sets the background color of the disabled dates.
        /// </summary>
        /// <value>The color of the disabled background.</value>
        public Color DisabledBackgroundColor
        {
            get { return (Color)GetValue(DisabledBackgroundColorProperty); }
            set { SetValue(DisabledBackgroundColorProperty, value); }
        }

        public static readonly BindableProperty DisabledTextColorProperty =
            BindableProperty.Create(nameof(DisabledTextColor), typeof(Color), typeof(Calendar), Color.FromHex("#dddddd"),
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDisabledTextColor((Color)newValue, (Color)oldValue));

        protected void ChangeDisabledTextColor(Color newValue, Color oldValue)
        {
            if (newValue == oldValue) return;
            buttons.FindAll(b => !b.IsEnabled).ForEach(b => b.TextColor = newValue);
        }

        /// <summary>
        /// Gets or sets the text color of the disabled dates.
        /// </summary>
        /// <value>The color of the disabled text.</value>
        public Color DisabledTextColor
        {
            get { return (Color)GetValue(DisabledTextColorProperty); }
            set { SetValue(DisabledTextColorProperty, value); }
        }

        public static readonly BindableProperty DisabledFontSizeProperty =
            BindableProperty.Create(nameof(DisabledFontSize), typeof(double), typeof(Calendar), 20.0,
                                    propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeDisabledFontSize((double)newValue, (double)oldValue));

        protected void ChangeDisabledFontSize(double newValue, double oldValue)
        {
            if (Math.Abs(newValue - oldValue) < 0.01) return;
            buttons.FindAll(b => !b.IsEnabled).ForEach(b => b.FontSize = newValue);
        }

        /// <summary>
        /// Gets or sets the font size of the disabled dates.
        /// </summary>
        /// <value>The size of the disabled font.</value>
        public double DisabledFontSize
        {
            get { return (double)GetValue(DisabledFontSizeProperty); }
            set { SetValue(DisabledFontSizeProperty, value); }
        }

        public static readonly BindableProperty DateCommandProperty =
            BindableProperty.Create(nameof(DateCommand), typeof(ICommand), typeof(Calendar), null);

        /// <summary>
        /// Gets or sets the selected date command.
        /// </summary>
        /// <value>The date command.</value>
        public ICommand DateCommand
        {
            get { return (ICommand)GetValue(DateCommandProperty); }
            set { SetValue(DateCommandProperty, value); }
        }

        public static readonly BindableProperty RightArrowCommandProperty =
            BindableProperty.Create(nameof(RightArrowCommand), typeof(ICommand), typeof(Calendar), null);

        public ICommand RightArrowCommand
        {
            get { return (ICommand)GetValue(RightArrowCommandProperty); }
            set { SetValue(RightArrowCommandProperty, value); }
        }

        public static readonly BindableProperty LeftArrowCommandProperty =
            BindableProperty.Create(nameof(LeftArrowCommand), typeof(ICommand), typeof(Calendar), null);

        public ICommand LeftArrowCommand
        {
            get { return (ICommand)GetValue(LeftArrowCommandProperty); }
            set { SetValue(LeftArrowCommandProperty, value); }
        }

		public static readonly BindableProperty SpecialDatesProperty = 
			BindableProperty.Create(nameof(SpecialDates), typeof(List<SpecialDate>), typeof(Calendar), null,
			                        propertyChanged: (bindable, oldValue, newValue) => (bindable as Calendar).ChangeCalendar(CalandarChanges.MaxMin));

		public List<SpecialDate> SpecialDates
		{
			get { return (List<SpecialDate>)GetValue(SpecialDatesProperty); }
			set { SetValue(SpecialDatesProperty, value); }
		}

		public void RaiseSpecialDatesChanged()
		{
			ChangeCalendar(CalandarChanges.MaxMin);
		}

		public DateTime CalendarStartDate
        {
            get
            {
                var start = StartDate;
                var beginOfMonth = start.Day == 1;
                while (!beginOfMonth || start.DayOfWeek != StartDay)
                {
                    start = start.AddDays(-1);
                    beginOfMonth |= start.Day == 1;
                }
                return start;
            }
        }

        protected void ChangeWeekdays()
        {
            if (!WeekdaysShow) return;
            var start = CalendarStartDate;
            for (int i = 0; i < dayLabels.Count; i++)
            {
                dayLabels[i].Text = start.ToString(WeekdaysFormat);
                start = start.AddDays(1);
            }
        }

		protected void ChangeWeekNumbers()
		{
			if (!ShowNumberOfWeek) return;
			CultureInfo ciCurr = CultureInfo.CurrentCulture;
			var start = StartDate;
			for (int i = 0; i < weekNumberLabels.Count; i++)
			{
				var weekNum = ciCurr.Calendar.GetWeekOfYear(start, CalendarWeekRule.FirstFourDayWeek, StartDay);
				weekNumberLabels[i].Text = string.Format("{0}", weekNum);
				start = start.AddDays(7);
			}
		}

        protected void ChangeCalendar(CalandarChanges changes)
        {
			Device.BeginInvokeOnMainThread(() =>
			{
				if (changes.HasFlag(CalandarChanges.StartDate))
				{
					CenterLabel.Text = StartDate.ToString(TitleLabelFormat);
					ChangeWeekNumbers();
				}

				var start = CalendarStartDate.Date;
				var beginOfMonth = false;
				var endOfMonth = false;
				for (int i = 0; i < buttons.Count; i++)
				{
					endOfMonth |= beginOfMonth && start.Day == 1;
					beginOfMonth |= start.Day == 1;

					if (i < 7 && WeekdaysShow && changes.HasFlag(CalandarChanges.StartDay))
					{
						dayLabels[i].Text = start.ToString(WeekdaysFormat);
					}

					if (changes.HasFlag(CalandarChanges.All))
					{
						buttons[i].Text = string.Format("{0}", start.Day);
					}
					else
					{
						buttons[i].TextWithoutMeasure = string.Format("{0}", start.Day);
					}
					buttons[i].Date = start;

					buttons[i].IsOutOfMonth = !(beginOfMonth && !endOfMonth);

					SpecialDate sd = null;
					if (SpecialDates != null)
					{
						sd = SpecialDates.FirstOrDefault(s => s.Date.Date == start.Date);
					}

					if ((MinDate.HasValue && start < MinDate) || (MaxDate.HasValue && start > MaxDate) || (DisableAllDates && sd == null))
					{
						SetButtonDisabled(buttons[i]);
					}
					else if (SelectedDates.Contains(start.Date))
					{
						SetButtonSelected(buttons[i], sd);
					}
					else if (sd != null)
					{
						SetButtonSpecial(buttons[i], sd);
					}
					else
					{
						SetButtonNormal(buttons[i]);
					}
					start = start.AddDays(1);
				}
			});
        }

        protected void SetButtonNormal(CalendarButton button)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                button.IsEnabled = true;
                button.IsSelected = false;
                button.FontSize = DatesFontSize;
                button.BorderWidth = BorderWidth;
                button.BorderColor = BorderColor;
                button.BackgroundColor = button.IsOutOfMonth ? DatesBackgroundColorOutsideMonth : DatesBackgroundColor;
                button.TextColor = button.IsOutOfMonth ? DatesTextColorOutsideMonth : DatesTextColor;
				button.FontAttributes = button.IsOutOfMonth ? DatesFontAttributesOutsideMonth : DatesFontAttributes;
            });
        }

		protected void SetButtonSelected(CalendarButton button, SpecialDate special)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
				var defaultBackgroundColor = button.IsOutOfMonth ? DatesBackgroundColorOutsideMonth : DatesBackgroundColor;
				var defaultTextColor = button.IsOutOfMonth ? DatesTextColorOutsideMonth : DatesTextColor;
				var defaultFontAttributes = button.IsOutOfMonth ? DatesFontAttributesOutsideMonth : DatesFontAttributes;
				button.IsEnabled = true;
                button.IsSelected = true;
                button.FontSize = SelectedFontSize;
                button.BorderWidth = SelectedBorderWidth;
                button.BorderColor = SelectedBorderColor;
				button.BackgroundColor = SelectedBackgroundColor.HasValue ? SelectedBackgroundColor.Value : (special != null && special.BackgroundColor.HasValue ? special.BackgroundColor.Value : defaultBackgroundColor);
				button.TextColor = SelectedTextColor.HasValue ? SelectedTextColor.Value : (special != null && special.TextColor.HasValue ? special.TextColor.Value : defaultTextColor);
				button.FontAttributes = SelectedFontAttributes != FontAttributes.None ? SelectedFontAttributes : (special != null && special.FontAttributes.HasValue ? special.FontAttributes.Value : defaultFontAttributes);
            });
        }

        protected void SetButtonDisabled(CalendarButton button)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                button.FontSize = DisabledFontSize;
                button.BorderWidth = DisabledBorderWidth;
                button.BorderColor = DisabledBorderColor;
                button.BackgroundColor = DisabledBackgroundColor;
                button.TextColor = DisabledTextColor;
                button.IsEnabled = false;
				button.IsSelected = false;
            });
        }

		protected void SetButtonSpecial(CalendarButton button, SpecialDate special)
		{
            Device.BeginInvokeOnMainThread(() =>
            {
                if (special.FontSize.HasValue) button.FontSize = special.FontSize.Value;
			    if (special.BorderWidth.HasValue) button.BorderWidth = special.BorderWidth.Value;
			    if (special.BorderColor.HasValue) button.BorderColor = special.BorderColor.Value;
			    if (special.BackgroundColor.HasValue) button.BackgroundColor = special.BackgroundColor.Value;
			    if (special.TextColor.HasValue) button.TextColor = special.TextColor.Value;
				if (special.FontAttributes.HasValue) button.FontAttributes = special.FontAttributes.Value;
			    button.IsEnabled = special.Selectable;
            });
        }

        protected void DateClickedEvent(object s, EventArgs a)
        {
			var selectedDate = (s as CalendarButton).Date;
			if (SelectedDate.HasValue && selectedDate.HasValue && SelectedDate.Value == selectedDate.Value)
			{
				ChangeSelectedDate(selectedDate);
				SelectedDate = null;
			}
			else 
			{
				SelectedDate = selectedDate;
			}
        }

		protected bool ChangeSelectedDate(DateTime? date)
        {
            if (!date.HasValue) return false;
            
			if (!MultiSelectDates)
			{
				buttons.FindAll(b => b.IsSelected).ForEach(b => ResetButton(b));
				SelectedDates.Clear();
			}

			var button = buttons.Find(b => b.Date.HasValue && b.Date.Value.Date == date.Value.Date);
			if (button == null) return false;
			var deselect = button.IsSelected;
			if (button.IsSelected)
			{
				ResetButton(button);
			}
			else
			{
				SelectedDates.Add(SelectedDate.Value.Date);
				var spD = SpecialDates?.FirstOrDefault(s => s.Date.Date == button.Date.Value.Date);
				SetButtonSelected(button, spD);
			}
			DateClicked?.Invoke(this, new DateTimeEventArgs { DateTime = SelectedDate.Value });
			DateCommand?.Execute(SelectedDate.Value);
			return deselect;
        }

		protected void ResetButton(CalendarButton b)
		{
			if (b.Date.HasValue) SelectedDates.Remove(b.Date.Value.Date);
			var spD = SpecialDates?.FirstOrDefault(s => s.Date.Date == b.Date.Value.Date);
			SetButtonNormal(b);
			if (spD != null)
			{
				SetButtonSpecial(b, spD);
			}
		}

        protected void LeftArrowClickedEvent(object s, EventArgs a)
        {
            StartDate = new DateTime(StartDate.Year, StartDate.Month, 1).AddMonths(-1);
            LeftArrowClicked?.Invoke(s, new DateTimeEventArgs { DateTime = StartDate });
            LeftArrowCommand?.Execute(StartDate);
        }

        protected void RightArrowClickedEvent(object s, EventArgs a)
        {
            StartDate = new DateTime(StartDate.Year, StartDate.Month, 1).AddMonths(1);
            RightArrowClicked?.Invoke(s, new DateTimeEventArgs { DateTime = StartDate });
            RightArrowCommand?.Execute(StartDate);
        }

        public event EventHandler<DateTimeEventArgs> RightArrowClicked, LeftArrowClicked, DateClicked;
    }

    public static class EnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
		{
			foreach (T item in enumeration)
			{
				action(item);
			}
		}
	}
}

