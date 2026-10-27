# 拾光壁纸 UI 资源索引

以下是编译资源的解析结果，不等同于原始源码；层级与常量可供复现参考，运行时变化需要另外验证。

- 资源候选：979
- 成功解析的 XAML 文件：39
- 文本资源：831

原始布局位于 `xaml/`；`ui-map.json` 保留节点父子关系、属性及 x:Uid 对应文本。

## 页面清单

| 文件 | XML 节点数 |
| --- | ---: |
| [xaml/Files/App.xaml](xaml/Files/App.xaml) | 3 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.Segmented/Segmented/Segmented.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.Segmented/Segmented/Segmented.xaml) | 70 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.Segmented/SegmentedItem/SegmentedItem.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.Segmented/SegmentedItem/SegmentedItem.xaml) | 577 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.Segmented/Themes/Generic.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.Segmented/Themes/Generic.xaml) | 4 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.SettingsControls/SettingsCard/SettingsCard.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.SettingsControls/SettingsCard/SettingsCard.xaml) | 604 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.SettingsControls/SettingsExpander/SettingsExpander.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.SettingsControls/SettingsExpander/SettingsExpander.xaml) | 379 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.SettingsControls/Themes/Generic.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.SettingsControls/Themes/Generic.xaml) | 4 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.TokenView/Themes/Generic.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.TokenView/Themes/Generic.xaml) | 4 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.TokenView/TokenItem/TokenItem.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.TokenView/TokenItem/TokenItem.xaml) | 289 |
| [xaml/Files/CommunityToolkit.WinUI.Controls.TokenView/TokenView/TokenView.xaml](xaml/Files/CommunityToolkit.WinUI.Controls.TokenView/TokenView/TokenView.xaml) | 117 |
| [xaml/Files/Dialogs/AccountDlg.xaml](xaml/Files/Dialogs/AccountDlg.xaml) | 36 |
| [xaml/Files/Dialogs/ActivityAlertDlg.xaml](xaml/Files/Dialogs/ActivityAlertDlg.xaml) | 5 |
| [xaml/Files/Dialogs/ActivityDlg.xaml](xaml/Files/Dialogs/ActivityDlg.xaml) | 8 |
| [xaml/Files/Dialogs/AdDlg.xaml](xaml/Files/Dialogs/AdDlg.xaml) | 8 |
| [xaml/Files/Dialogs/BbsDlg.xaml](xaml/Files/Dialogs/BbsDlg.xaml) | 8 |
| [xaml/Files/Dialogs/ChatGroupDlg.xaml](xaml/Files/Dialogs/ChatGroupDlg.xaml) | 5 |
| [xaml/Files/Dialogs/DonateDlg.xaml](xaml/Files/Dialogs/DonateDlg.xaml) | 8 |
| [xaml/Files/Dialogs/DownFav2Dlg.xaml](xaml/Files/Dialogs/DownFav2Dlg.xaml) | 15 |
| [xaml/Files/Dialogs/DownFavDlg.xaml](xaml/Files/Dialogs/DownFavDlg.xaml) | 14 |
| [xaml/Files/Dialogs/DownGluttonDlg.xaml](xaml/Files/Dialogs/DownGluttonDlg.xaml) | 9 |
| [xaml/Files/Dialogs/FavDlg.xaml](xaml/Files/Dialogs/FavDlg.xaml) | 8 |
| [xaml/Files/Dialogs/FunLimitDlg.xaml](xaml/Files/Dialogs/FunLimitDlg.xaml) | 5 |
| [xaml/Files/Dialogs/PrivacyDlg.xaml](xaml/Files/Dialogs/PrivacyDlg.xaml) | 6 |
| [xaml/Files/Dialogs/ProDlg.xaml](xaml/Files/Dialogs/ProDlg.xaml) | 6 |
| [xaml/Files/Dialogs/PushDlg.xaml](xaml/Files/Dialogs/PushDlg.xaml) | 8 |
| [xaml/Files/Dialogs/QuizDlg.xaml](xaml/Files/Dialogs/QuizDlg.xaml) | 8 |
| [xaml/Files/Dialogs/QuizSubmitDlg.xaml](xaml/Files/Dialogs/QuizSubmitDlg.xaml) | 21 |
| [xaml/Files/Dialogs/R22Dlg.xaml](xaml/Files/Dialogs/R22Dlg.xaml) | 10 |
| [xaml/Files/Dialogs/ReqDonateDlg.xaml](xaml/Files/Dialogs/ReqDonateDlg.xaml) | 5 |
| [xaml/Files/Dialogs/SeekDlg.xaml](xaml/Files/Dialogs/SeekDlg.xaml) | 8 |
| [xaml/Files/Dialogs/ShortcutDlg.xaml](xaml/Files/Dialogs/ShortcutDlg.xaml) | 6 |
| [xaml/Files/Dialogs/SubscribeAutoDlg.xaml](xaml/Files/Dialogs/SubscribeAutoDlg.xaml) | 5 |
| [xaml/Files/Dialogs/SubscribeDlg.xaml](xaml/Files/Dialogs/SubscribeDlg.xaml) | 5 |
| [xaml/Files/Dialogs/SubscribeRefundDlg.xaml](xaml/Files/Dialogs/SubscribeRefundDlg.xaml) | 5 |
| [xaml/Files/Dialogs/TileDlg.xaml](xaml/Files/Dialogs/TileDlg.xaml) | 8 |
| [xaml/Files/Dialogs/ToastDlg.xaml](xaml/Files/Dialogs/ToastDlg.xaml) | 8 |
| [xaml/Files/Microsoft.UI.Xaml/DensityStyles/Compact.xaml](xaml/Files/Microsoft.UI.Xaml/DensityStyles/Compact.xaml) | 3 |
| [xaml/Files/Pages/MainPage.xaml](xaml/Files/Pages/MainPage.xaml) | 114 |
| [xaml/Files/Pages/SettingsView.xaml](xaml/Files/Pages/SettingsView.xaml) | 1037 |

## MainPage 命名控件

| 类型 | 名称 | 资源文本 | 布局属性 |
| --- | --- | --- | --- |
| TeachingTip | TipShare |  |  |
| Image | ImgShare |  |  |
| TeachingTip | TipBeforeDl | ActionButtonContent: Donate; CloseButtonContent: Watched; Subtitle: Download credits used up. Scan qrcode above to download the picture. |  |
| Image | ImgBeforeDl |  |  |
| TeachingTip | TipBeforeDlAnon | ActionButtonContent: Bind; CloseButtonContent: Watched; Subtitle: Scan qrcode above to download the picture. |  |
| Image | ImgBeforeDlAnon |  |  |
| Flyout | FlyoutGo |  |  |
| TextBox | BoxGo | Header: Look back | MaxWidth=320 |
| Run | TextGoDesc | Text: Documentation here. |  |
| Run | TextView | Text: View |  |
| RelativePanel | ImgUhdPointer |  | Background=#00FFFFFF |
| Image | ImgUhd1 |  |  |
| Image | ImgUhd2 |  |  |
| SplitView | ViewSplit |  | DisplayMode=Overlay; OpenPaneLength=450 |
| SettingsView | ViewSettings |  |  |
| RelativePanel | ViewMain |  | Background=#00FFFFFF |
| RelativePanel | ViewBarPointer |  | Background=#00FFFFFF |
| StackPanel | ViewBar |  | Background={ThemeResource AcrylicInAppFillColorDefaultBrush}; MaxWidth=640; Margin=30; Padding=16,12; CornerRadius=14 |
| TextBlock | TextTitle |  | Style={StaticResource SubtitleTextBlockStyle} |
| ProgressBar | ProgressLoading |  | Margin=0,8,0,0 |
| StackPanel | ViewStory |  | Visibility=Collapsed |
| TextBlock | TextDetailCaption |  | Style={StaticResource BodyTextBlockStyle}; Visibility=Collapsed; Margin=0,8,0,0 |
| TextBlock | TextDetailStory |  | Style={StaticResource BodyTextBlockStyle}; Margin=0,8,0,0; Visibility=Collapsed |
| TextBlock | TextDetailCopyright |  | Style={StaticResource CaptionTextBlockStyle}; Margin=0,8,0,0; Visibility=Collapsed |
| TextBlock | TextDetailDate |  | Style={StaticResource CaptionTextBlockStyle}; Visibility=Collapsed; Margin=0,8,0,0 |
| TextBlock | TextDetailProperties |  | Style={StaticResource CaptionTextBlockStyle}; Visibility=Collapsed; Margin=0,8,0,0 |
| StackPanel | ViewInfo |  | Orientation=Vertical; Margin=29,44,29,0 |
| InfoBar | Info |  | Margin=0,0,0,8 |
| Rectangle | AnchorGo |  | Width=0; Height=0; Margin=24,28 |

## SettingsView 命名控件

| 类型 | 名称 | 资源文本 | 布局属性 |
| --- | --- | --- | --- |
| Flyout | FlyoutTopic |  |  |
| StackPanel | PanelTopic |  | Width=374 |
| TextBlock | TextTopicTitle | Text: Select topic | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,0,0,8 |
| TokenView | TokenTopic |  | Margin=0,6,0,0 |
| StackPanel | PanelReward |  | Width=374 |
| StackPanel | PanelProvider |  | Width=374 |
| StackPanel | PanelDevice |  | Width=374 |
| Storyboard | AnimeSubscribe |  |  |
| Storyboard | AnimeDonate |  |  |
| StackPanel | PanelTitleSettings |  | Orientation=Vertical; Margin=21,32,20,30 |
| TextBlock | TextSettings | Text: Settings | Style={StaticResource TitleLargeTextBlockStyle} |
| Segmented | SegmentedWindow |  |  |
| SegmentedItem | SegmentedWindowFull | ToolTipService/ToolTip: Fullscreen mode (Enter) |  |
| SegmentedItem | SegmentedWindowNarrow | ToolTipService/ToolTip: Narrow-screen mode (F8) |  |
| SegmentedItem | SegmentedWindowWide | ToolTipService/ToolTip: Widescreen mode (F8) |  |
| TextBlock | TextGlitter |  | Style={StaticResource BodyTextBlockStyle}; Margin=0,8,0,0 |
| Hyperlink | LinkGlitter |  |  |
| Run | TextGlitterSrc | Text: GLITTER  |  |
| Run | TextGlitterSentence | Text: 时光如歌，岁月如诗 |  |
| ScrollViewer | ScrollSettings |  |  |
| StackPanel | ViewSettings |  | Padding=20,0,20,30 |
| StackPanel | ViewInfo |  | Visibility=Collapsed; Margin=0,0,0,24 |
| SettingsExpander | ExpanderAccount | Description: Bind account to unlock more features; Header: Account |  |
| SettingsCard | SettingsAccountCheckin | Description: Get 1 point every day; Header: Claim points |  |
| Button | BtnAccountCheckin | Content: Check-in |  |
| SettingsCard | SettingsAccountAd | Description: Get 2 points every time; Header: Earn points |  |
| Button | BtnAccountAd | Content: View ad |  |
| SettingsCard | SettingsAccountDonate | Description: Get 10 times the amount in points; Header: Get points |  |
| Button | BtnAccountDonate | Content: Donate |  |
| SettingsCard | SettingsAccountReward | Header: Points record | Visibility=Collapsed |
| SettingsCard | SettingsAccountDevice | Header: My devices | Visibility=Collapsed |
| SettingsCard | SettingsAccountBind | Header: Bind account |  |
| SettingsCard | SettingsAccountDesc | Header: Learn more about account |  |
| Button | BtnAccount |  | Style={StaticResource AccentButtonStyle} |
| SettingsExpander | ExpanderSubscribe | Description: Subscribe for auto wallpaper; Header: Subscription | Margin=0,6,0,0 |
| FontIcon | IconSubscribe |  |  |
| SettingsCard | SettingsSubscribeDesc | Header: Enjoy auto desktop and lock screen changing, and more during your subscription. |  |
| SettingsCard | SettingsSubscribe1 | Header: Subscribe for 30 days (30 points) |  |
| SettingsCard | SettingsSubscribe2 | Header: Subscribe for 180 days (150 points) |  |
| SettingsCard | SettingsSubscribeAuto | Header: Auto-renewal at expiration | Visibility=Collapsed |
| ToggleSwitch | TsSubscribeAuto |  |  |
| SettingsCard | SettingsSubscribeRefund | Header: Cancel subscription |  |
| Button | BtnSubscribe |  | Style={StaticResource AccentButtonStyle} |
| SettingsExpander | ExpanderPushDesktop | Header: Auto desktop | Margin=0,6,0,0 |
| InfoBar | InfoPushDesktop | Message: Service denied. | CornerRadius=0 |
| SettingsCard | SettingsPushDesktopPeriod | Header: Period |  |
| ComboBox | BoxPushDesktopPeriod |  |  |
| SettingsCard | SettingsPushDesktopNow | Header: Change now |  |
| SettingsCard | SettingsPushDesktopAuth | Header: If desktop background doesn't change automatically, check docs for help. | Visibility=Collapsed |
| ComboBox | BoxPushDesktop |  |  |
| SettingsExpander | ExpanderPushLock | Header: Auto lock screen | Margin=0,6,0,0 |
| InfoBar | InfoPushLock | Message: Service denied. | CornerRadius=0 |
| SettingsCard | SettingsPushLockPeriod | Header: Period |  |
| ComboBox | BoxPushLockPeriod |  |  |
| SettingsCard | SettingsPushLockNow | Header: Change now |  |
| SettingsCard | SettingsPushLockAuth | Header: If lock screen background doesn't change automatically, check docs for help. | Visibility=Collapsed |
| ComboBox | BoxPushLock |  |  |
| SettingsExpander | ExpanderToastDaily | Description: Get daily wallpaper notifications; Header: Daily wallpaper | Margin=0,6,0,0 |
| InfoBar | InfoToastDaily |  | CornerRadius=0 |
| SettingsCard | SettingsToastDailyNow | Header: Push now |  |
| SettingsCard | SettingsToastDailyAuth |  | Visibility=Collapsed |
| ComboBox | BoxToastDaily |  |  |
| SettingsExpander | ExpanderBbs | Description: Messages from the developer; Header: News | Margin=0,6,0,0 |
| SettingsCard | SettingsBbsWeb | Header: Read more... |  |
| SettingsExpander | ExpanderActivity | Description: Earn points and more; Header: Events | Margin=0,6,0,0 |
| SettingsCard | SettingsActivityAlert | Header: Notify for new events |  |
| ToggleSwitch | TsActivityAlert |  |  |
| SettingsCard | SettingsActivityWeb | Header: Read more... |  |
| TextBlock | TextSettingsProvider0 | Text: My providers | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8 |
| SettingsExpander | ExpanderCache |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsCacheSource | Header: Source |  |
| Segmented | SegmentedCacheSource |  |  |
| SegmentedItem | SegmentedCacheSourceDesktop | Content: Desktop |  |
| SegmentedItem | SegmentedCacheSourceLock | Content: Lock screen |  |
| SettingsExpander | ExpanderLocal |  | Margin=0,6,0,0 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedLocalOrder |  |  |
| SettingsCard | SettingsLocalFolder | Header: Organize gallery | Padding=16,8 |
| SettingsCard | SettingsLocalPick | Header: Change folder | Padding=16,8 |
| SettingsCard | SettingsLocalGlutton | Header: Get weekly picks | Padding=16,8 |
| SettingsExpander | ExpanderFav |  | Margin=0,6,0,0 |
| SettingsCard | SettingsFavLimit | Header: You are using temporary favorites. For long-term storage and multi-device sync, sign in to save your favorites. | Padding=16,8 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedFavOrder |  |  |
| SettingsCard | SettingsFavWeb | Header: Favorite web | Padding=16,8 |
| SettingsCard | SettingsFavDl | Header: Save all images | Padding=16,8 |
| SettingsCard | SettingsFavR18 | Header: View NSFW | Padding=16,8 |
| SettingsExpander | ExpanderSeek |  | Margin=0,6,0,0 |
| SettingsCard | SettingsSeekLimit | Header: For You is not enabled. No content available. To use this service, please subscribe. | Padding=16,8 |
| SettingsCard | SettingsSeekDesc | Header: While browsing images, adding favorites and marking DISLIKE will train Timeline to recommend better content for you. | Visibility=Collapsed |
| SettingsCard | SettingsSeekHow | Header: How does Timeline recommend content for me? | Padding=16,8 |
| SettingsCard | SettingsSeekR18 | Header: View NSFW | Padding=16,8 |
| TextBlock | TextSettingsProvider1 | Text: Recommended providers | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8 |
| SettingsExpander | ExpanderGlutton |  | Margin=0,6,0,0 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedGluttonOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedGluttonCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedGluttonCateWhat |  |  |
| SettingsCard | SettingsTone | Header: Tone |  |
| Segmented | SegmentedGluttonTone |  |  |
| SettingsExpander | ExpanderTrending |  | Margin=0,6,0,0 |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedTrendingCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedTrendingCateWhat |  |  |
| SettingsCard | SettingsTone | Header: Tone |  |
| Segmented | SegmentedTrendingTone |  |  |
| SettingsExpander | ExpanderTopic |  | Margin=0,6,0,0 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedTopicOrder |  |  |
| SettingsCard | SettingsTopic | Header: Topic |  |
| DropDownButton | BtnTopic |  |  |
| SettingsCard | SettingsTopicWeb | Header: Topic web | Padding=16,8 |
| SettingsExpander | ExpanderSnake |  | Margin=0,6,0,0 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedSnakeOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedSnakeCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedSnakeCateWhat |  |  |
| SettingsCard | SettingsTone | Header: Tone |  |
| Segmented | SegmentedSnakeTone |  |  |
| TextBlock | TextSettingsProvider2 | Text: Daily providers | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8 |
| SettingsExpander | ExpanderSpotlight |  | Margin=0,6,0,0 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedSpotlightOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedSpotlightCateWhat |  |  |
| SettingsExpander | ExpanderBing |  | Margin=0,6,0,0 |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedBingOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedBingCateWhat |  |  |
| SettingsExpander | ExpanderTimeline |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedTimelineOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedTimelineCateWhat |  |  |
| SettingsExpander | ExpanderQuiz |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsQuizDesc | Header: Guess the daily scenic spot! (Submit via right-click menu) Correct guesses earn 3 points. |  |
| SettingsCard | SettingsQuizStats |  | Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedQuizOrder |  |  |
| SettingsExpander | ExpanderNasa |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedNasaOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedNasaCateHow |  |  |
| SettingsExpander | ExpanderNasaimages |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedNasaimagesOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedNasaimagesCateHow |  |  |
| SettingsExpander | ExpanderOne |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedOneOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedOneCateWhat |  |  |
| SettingsExpander | ExpanderHimawari8 |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsHimawari8Offset | Header: Earth position |  |
| Button | BtnHimawari8OffsetYMinus | Content: T |  |
| Button | BtnHimawari8OffsetYPlus | Content: D | Margin=8,0,0,0 |
| Button | BtnHimawari8OffsetXMinus | Content: L | Margin=8,0,0,0 |
| Button | BtnHimawari8OffsetXPlus | Content: R | Margin=8,0,0,0 |
| SettingsCard | SettingsHimawari8Scale | Header: Earth size |  |
| Button | BtnHimawari8ScaleMinus | Content: - |  |
| Button | BtnHimawari8ScalePlus | Content: + | Margin=8,0,0,0 |
| SettingsExpander | ExpanderFy4b |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsFy4bOffset | Header: Earth position |  |
| Button | BtnFy4bOffsetYMinus | Content: T |  |
| Button | BtnFy4bOffsetYPlus | Content: D | Margin=8,0,0,0 |
| Button | BtnFy4bOffsetXMinus | Content: L | Margin=8,0,0,0 |
| Button | BtnFy4bOffsetXPlus | Content: R | Margin=8,0,0,0 |
| SettingsCard | SettingsFy4bScale | Header: Earth size |  |
| Button | BtnFy4bScaleMinus | Content: - |  |
| Button | BtnFy4bScalePlus | Content: + | Margin=8,0,0,0 |
| SettingsExpander | ExpanderFy2h |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsFy2hOffset | Header: Earth position |  |
| Button | BtnFy2hOffsetYMinus | Content: T |  |
| Button | BtnFy2hOffsetYPlus | Content: D | Margin=8,0,0,0 |
| Button | BtnFy2hOffsetXMinus | Content: L | Margin=8,0,0,0 |
| Button | BtnFy2hOffsetXPlus | Content: R | Margin=8,0,0,0 |
| SettingsCard | SettingsFy2hScale | Header: Earth size |  |
| Button | BtnFy2hScaleMinus | Content: - |  |
| Button | BtnFy2hScalePlus | Content: + | Margin=8,0,0,0 |
| TextBlock | TextSettingsProvider3 | Text: Other providers | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8; Visibility=Collapsed |
| SettingsExpander | ExpanderWallhaven |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedWallhavenOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedWallhavenCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedWallhavenCateWhat |  |  |
| SettingsExpander | ExpanderUnsplash |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedUnsplashOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedUnsplashCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedUnsplashCateWhat |  |  |
| SettingsExpander | ExpanderYmyouli |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedYmyouliOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedYmyouliCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedYmyouliCateWhat |  |  |
| SettingsExpander | ExpanderNetbian |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedNetbianOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedNetbianCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedNetbianCateWhat |  |  |
| SettingsExpander | ExpanderDpm |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedDpmOrder |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedDpmCateWhat |  |  |
| SettingsExpander | ExpanderQingbz |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedQingbzOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedQingbzCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedQingbzCateWhat |  |  |
| SettingsExpander | ExpanderZzzmh |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedZzzmhOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedZzzmhCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedZzzmhCateWhat |  |  |
| SettingsExpander | ExpanderToopic |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedToopicOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedToopicCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedToopicCateWhat |  |  |
| SettingsExpander | ExpanderBizhihui |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedBizhihuiOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedBizhihuiCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedBizhihuiCateWhat |  |  |
| SettingsExpander | ExpanderWallhere |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedWallhereOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedWallhereCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedWallhereCateWhat |  |  |
| SettingsExpander | ExpanderHuamao |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedHuamaoOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedHuamaoCateWhat |  |  |
| SettingsExpander | Expander3g |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | Segmented3gOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | Segmented3gCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | Segmented3gCateWhat |  |  |
| SettingsExpander | ExpanderWallpaperup |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedWallpaperupOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedWallpaperupCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedWallpaperupCateWhat |  |  |
| SettingsExpander | ExpanderColorhub |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedColorhubOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedColorhubCateWhat |  |  |
| SettingsExpander | ExpanderPexels |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedPexelsOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedPexelsCateWhat |  |  |
| SettingsExpander | ExpanderBizhi1 |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedBizhi1Order |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedBizhi1CateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedBizhi1CateWhat |  |  |
| SettingsExpander | ExpanderSimple |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedSimpleOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedSimpleCateWhat |  |  |
| SettingsExpander | ExpanderWeilan |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedWeilanOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedWeilanCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedWeilanCateWhat |  |  |
| SettingsExpander | ExpanderAbyss |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedAbyssOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedAbyssCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedAbyssCateWhat |  |  |
| SettingsExpander | ExpanderPixiv |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedPixivOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedPixivCateWhat |  |  |
| SettingsExpander | ExpanderPixabay |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedPixabayOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedPixabayCateWhat |  |  |
| SettingsExpander | ExpanderBackiee |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedBackieeOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedBackieeCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedBackieeCateWhat |  |  |
| SettingsExpander | ExpanderEso |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedEsoOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedEsoCateHow |  |  |
| SettingsExpander | ExpanderSkitter |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedSkitterOrder |  |  |
| SettingsCard | SettingsCate | Header: Category |  |
| Segmented | SegmentedSkitterCateWhat |  |  |
| SettingsExpander | ExpanderObzhi |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedObzhiOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedObzhiCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedObzhiCateWhat |  |  |
| SettingsExpander | ExpanderAdwest |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedAdwestOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedAdwestCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedAdwestCateWhat |  |  |
| SettingsExpander | ExpanderWander |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsOrder | Header: Sort by |  |
| Segmented | SegmentedWanderOrder |  |  |
| SettingsCard | SettingsCateHow | Header: Category |  |
| Segmented | SegmentedWanderCateHow |  |  |
| SettingsCard | SettingsCateWhat | Header: Subclass |  |
| Segmented | SegmentedWanderCateWhat |  |  |
| TextBlock | TextSettingsProvider4 | Text: Restricted providers | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8; Visibility=Collapsed |
| SettingsExpander | ExpanderFavR18 |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsFavR18Limit | Header: My Favorite (R18) contains NSFW content and requires an account to unlock. | Padding=16,8 |
| SettingsCard | SettingsFavR18Order | Header: Sort by |  |
| Segmented | SegmentedFavR18Order |  |  |
| SettingsCard | SettingsFavR18Web | Header: Favorite web | Padding=16,8 |
| SettingsExpander | ExpanderSeekR18 |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsSeekR18Limit1 | Header: For You (R18) contains NSFW content and requires an account to unlock. | Padding=16,8 |
| SettingsCard | SettingsSeekR18Limit2 | Header: For You (R18) contains NSFW content and requires authorization to unlock. Please do not distribute it illegally after unlocking, and take responsibility for any risks. | Padding=16,8 |
| SettingsCard | SettingsSeekR18Limit3 | Header: For You (R18) is not enabled. No content available. To use this service, please subscribe. | Padding=16,8 |
| SettingsCard | SettingsSeekR18Desc | Header: While browsing images, adding favorites and marking DISLIKE will train Timeline to recommend better content for you. | Visibility=Collapsed |
| SettingsCard | SettingsSeekR18How | Header: How does Timeline recommend content for me? | Padding=16,8 |
| SettingsExpander | ExpanderTopicR18 |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsTopicR18Limit1 | Header: Timeline Topic (R18) contains NSFW content and requires an account to unlock. | Padding=16,8 |
| SettingsCard | SettingsTopicR18Limit2 | Header: Timeline Topic (R18) contains NSFW content and requires authorization to unlock. Please do not distribute it illegally after unlocking, and take responsibility for any risks. | Padding=16,8 |
| SettingsCard | SettingsTopicR18Order | Header: Sort by |  |
| Segmented | SegmentedTopicR18Order |  |  |
| SettingsCard | SettingsTopicR18 | Header: Topic |  |
| ComboBox | BoxTopicR18Topic |  |  |
| SettingsCard | SettingsTopicR18Web | Header: Topic web | Padding=16,8 |
| SettingsExpander | ExpanderLamia |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsLamiaLimit1 | Header: Timeline Collection (R18) contains NSFW content and requires an account to unlock. | Padding=16,8 |
| SettingsCard | SettingsLamiaLimit2 | Header: Timeline Collection (R18) contains NSFW content and requires authorization to unlock. Please do not distribute it illegally after unlocking, and take responsibility for any risks. | Padding=16,8 |
| SettingsCard | SettingsLamiaOrder | Header: Sort by |  |
| Segmented | SegmentedLamiaOrder |  |  |
| SettingsCard | SettingsLamiaCate | Header: Category |  |
| Segmented | SegmentedLamiaCateHow |  |  |
| SettingsExpander | ExpanderLsp |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsLspLimit | Header: LSP contains NSFW content and requires an account to unlock. Please do not distribute it illegally after unlocking, and take responsibility for any risks. | Padding=16,8 |
| SettingsCard | SettingsLspOrder | Header: Sort by |  |
| Segmented | SegmentedLspOrder |  |  |
| SettingsCard | SettingsLspCate | Header: Category |  |
| Segmented | SegmentedLspCateHow |  |  |
| SettingsExpander | ExpanderVenus |  | Margin=0,6,0,0; Visibility=Collapsed |
| SettingsCard | SettingsVenusLimit1 | Header: Venus contains NSFW content and requires an account to unlock. | Padding=16,8 |
| SettingsCard | SettingsVenusLimit2 | Header: Venus contains NSFW content and requires authorization to unlock. Please do not distribute it illegally after unlocking, and take responsibility for any risks. | Padding=16,8 |
| SettingsCard | SettingsVenusOrder | Header: Sort by |  |
| Segmented | SegmentedVenusOrder |  |  |
| SettingsCard | SettingsVenusCate | Header: Category |  |
| Segmented | SegmentedVenusCateHow |  |  |
| SettingsCard | ExpanderProvider | Description: Add or remove 40+ providers; Header: Manage providers | Margin=0,6,0,0 |
| TextBlock | TextSettingsProvider5 | Text: Excluded providers | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8; Visibility=Collapsed |
| TextBlock | TextSettingsGeneral | Text: General | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8 |
| SettingsExpander | ExpanderTheme | Description: Select which theme to display; Header: App theme | Margin=0,6,0,0 |
| RadioButtons | RbTheme |  |  |
| RadioButton | RbThemeLight | Content: Light |  |
| RadioButton | RbThemeDark | Content: Dark |  |
| RadioButton | RbThemeSys | Content: Use system |  |
| TextBlock | TextThemeCur |  |  |
| SettingsExpander | ExpanderLang | Description: Change the language displayed in the app; Header: Display language | Visibility=Collapsed; Margin=0,6,0,0 |
| SettingsCard | SettingsLangWhy | Header: Why my language isn't supported? |  |
| SettingsCard | SettingsLangContribute | Header: Unfortunately it's impossible to translate the application into all languages because I do not speak all of them. However, if you would like to contribute by translating the application, please contact me. I would be happy to integrate your contribution. |  |
| ComboBox | BoxLang |  |  |
| SettingsExpander | ExpanderDig | Description: History desktop / lock screen wallpapers; Header: Wallpaper cache | Margin=0,6,0,0 |
| SettingsCard | SettingsCacheSource | Header: Source |  |
| Segmented | SegmentedDigSource |  |  |
| SegmentedItem | SegmentedCacheSourceDesktop | Content: Desktop |  |
| SegmentedItem | SegmentedCacheSourceLock | Content: Lock screen |  |
| SettingsCard | SettingsDigToTop | Header: Pin to: My providers |  |
| SettingsExpander | ExpanderFunc | Description: Toggle ads and more; Header: Feature toggles | Margin=0,6,0,0 |
| SettingsCard | SettingsFuncTile | Header: Live tile |  |
| Segmented | SegmentedFuncTile |  |  |
| SegmentedItem | SegmentedFuncTile0 | Content: Off |  |
| SegmentedItem | SegmentedFuncTile1 | Content: Auto |  |
| SegmentedItem | SegmentedFuncTile2 | Content: Manual |  |
| SettingsCard | SettingsFuncBbs | Header: News |  |
| ToggleSwitch | TsFuncBbs |  |  |
| SettingsCard | SettingsFuncActivity | Header: Events |  |
| ToggleSwitch | TsFuncActivity |  |  |
| SettingsCard | SettingsFuncAd | Header: Advertisements |  |
| ToggleSwitch | TsFuncAd |  |  |
| SettingsCard | ExpanderIni | Description: Experimental feature; Header: Configuration file | Margin=0,6,0,0 |
| Button | BtnIni | Content: Open |  |
| TextBlock | TextSettingsSupport | Text: Support | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8 |
| SettingsExpander | ExpanderDonate | Description: Nice to meet you; Header: Support us | Margin=0,6,0,0 |
| FontIcon | IconDonate |  |  |
| SettingsCard | SettingsDonate1 | Header: I designed Timeline in my limited spare time. If you enjoy it and are able, any small donation would help keep it running. |  |
| SettingsCard | SettingsDonate2 | Header: You can also rate it on the store to share with more wallpaper lovers. |  |
| SettingsCard | SettingsDonate3 | Header: You can also watch ads to support us indirectly. |  |
| Button | BtnDonate | Content: Donate |  |
| SettingsExpander | ExpanderThird | Description: Some great third-party apps; Header: Third-party | Margin=0,6,0,0 |
| SettingsCard | SettingsThirdFlipClock | Description: A compact full-screen clock tool (can also be used to keep your PC awake); Header: Screen Flip Clock |  |
| SettingsCard | SettingsThirdDynamicTheme | Description: A pure and elegant wallpaper app; Header: Dynamic Theme |  |
| SettingsCard | SettingsThirdYcbzjx | Description: Third-party wallpaper picks, stylish & handy; Header: 原创壁纸精选 |  |
| SettingsCard | SettingsThirdMore | Header: Let's cross-promote |  |
| SettingsExpander | ExpanderAd | Description: Ads you might like; Header: Advertisements | Margin=0,6,0,0 |
| SettingsCard | SettingsAdDef | Description: Supplementary text description; Header: Ad sample |  |
| SettingsCard | SettingsAdContact | Header: Advertise with us |  |
| TextBlock | TextAbout | Text: About | Style={StaticResource BodyStrongTextBlockStyle}; Margin=1,30,0,8 |
| SettingsExpander | ExpanderPlatform | Description: Web, mobile, etc.; Header: Cross platform | Margin=0,6,0,0 |
| SettingsCard | SettingsWeb | Header: Timeline web |  |
| SettingsCard | SettingsMini | Header: Timeline mobile |  |
| SettingsCard | SettingsShortcut | Header: iOS shortcut |  |
| SettingsCard | SettingsMac | Header: Other version |  |
| Button | BtnPlatform | Content: Web |  |
| SettingsCard | ExpanderDoc | Description: Help & FAQs; Header: Timeline document | Margin=0,6,0,0 |
| SettingsExpander | ExpanderContact | Description: Developer Q&A; Header: Contact me | Margin=0,6,0,0 |
| SettingsCard | SettingsChatGroup | Header: Join WeChat / QQ group |  |
| SettingsCard | SettingsContact | Description: nguaduot@163.com, etc.; Header: More contact info |  |
| SettingsExpander | ExpanderAbout | Description: © 2021-2026 nguaduot. All rights reserved. | Margin=0,6,0,0 |
| SettingsCard | SettingsStore | Header: View on Microsoft Store |  |
| SettingsCard | SettingsPro | Header: Upgrade to Pro | Visibility=Collapsed |
| SettingsCard | SettingsChangelog | Header: View changelog |  |
| SettingsCard | SettingsHomepage | Header: Homepage |  |
| SettingsCard | SettingsPrivacy | Header: Terms of use |  |
| Button | BtnRelease | Content: New | Style={StaticResource AccentButtonStyle}; Visibility=Collapsed |

## 菜单文案（只能证明资源存在，不能证明运行时位置）

| 资源键 | 文案 |
| --- | --- |
| MenuAd | More ads |
| MenuAdContact | Advertise with us |
| MenuAdDef | Ad sample |
| MenuAdOff | Turn off ads |
| MenuAdTopDef | Ad sample |
| MenuDislike/Label | Mark as disliked |
| MenuDownload/Label | Download |
| MenuFillOff/Label | Uniform |
| MenuFillOn/Label | Uniform to fill |
| MenuNext/Label | Next |
| MenuProvider/Label | Switch provider |
| MenuProviderMore/Text | Click Settings to swith more |
| MenuPushDesktop/Label | Auto desktop |
| MenuPushLock/Label | Auto lock screen |
| MenuQuiz/Label | Guess where |
| MenuSave/Label | Add to favorites |
| MenuSetDesktop/Label | Set as desktop |
| MenuSetLock/Label | Set as lock screen |
| MenuSetTile/Label | Set as tile |
| MenuSettings/Label | Settings |
| MenuShare/ToolTipService/ToolTip | Share |
