using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Measured, bounded visual repositioning for original native widgets.
    /// Uses the same safe area as the CU guided World/House/Stats screens:
    /// 91 px header, optional 226 px rail, optional 338 px context, 81 px
    /// bottom controls. No hardcoded native widget tree positions.
    /// </summary>
    public static class CUNativeLayoutPolicy
    {
        public const int HeaderBottom = 96;
        public const int ContentTop = 116;
        public const int FooterReserve = 93;
        public const int CompactLeftInset = 28;
        public const int WideLeftInset = 260;
        public const int WideRightInset = 358;
        public const int CompactRightInset = 28;
        public const int WideThreshold = 1600;
        public const int MinimumWidth = 1280;
        public const int MinimumHeight = 720;
        public const int MaxContentShift = 256;
        public const int MaxFooterShift = 64;

        public static bool IsViewportSupported(int width,int height)
        {
            return width>=MinimumWidth && height>=MinimumHeight &&
                width<=7680 && height<=4320;
        }

        public static bool CanMovePrimaryPanel(string stage)
        {
            // Never shift banner shields/colors or genuine FaceGen scenes.
            // Culture owns its already CU-styled full-screen movie.
            return stage=="CharacterCreationNarrativeStage" ||
                stage=="CharacterCreationClanNamingStage" ||
                stage=="CharacterCreationReviewStage" ||
                stage=="CharacterCreationOptionsStage";
        }

        public static bool CanAlignNativeFooter(string stage)
        {
            return CUNativeWidgetSkinPolicy.CanSkin(stage);
        }

        public static float ContentLeft(int width)
        {
            return width>=WideThreshold?WideLeftInset:CompactLeftInset;
        }

        public static float ContentRight(int width)
        {
            return width>=WideThreshold?
                width-WideRightInset:width-CompactRightInset;
        }

        public static float ContentBottom(int height)
        {
            return height-FooterReserve;
        }

        public static bool FitsPrimary(string stage,
            int screenWidth,int screenHeight,
            float x,float y,float width,float height)
        {
            if(!CanMovePrimaryPanel(stage) ||
                !IsViewportSupported(screenWidth,screenHeight) ||
                !Valid(x)||!Valid(y)||!Valid(width)||!Valid(height))
                return false;
            float left=ContentLeft(screenWidth);
            float right=ContentRight(screenWidth);
            float top=ContentTop;
            float bottom=ContentBottom(screenHeight);
            return width>=320 && height>=130 &&
                x>=left && y>=top &&
                x+width<=right && y+height<=bottom;
        }

        /// <summary>
        /// The only allowed content correction is a small translation
        /// computed from ACTUAL measured bounds. If a panel cannot fit the
        /// available region, or requires a large correction, leave it native.
        /// </summary>
        public static bool TryPlanPrimary(string stage,
            int screenWidth,int screenHeight,
            float x,float y,float width,float height,
            out float moveX,out float moveY)
        {
            moveX=0;
            moveY=0;
            if(!CanMovePrimaryPanel(stage) ||
                !IsViewportSupported(screenWidth,screenHeight) ||
                !Valid(x)||!Valid(y)||!Valid(width)||!Valid(height))
                return false;
            float left=ContentLeft(screenWidth);
            float right=ContentRight(screenWidth);
            float top=ContentTop;
            float bottom=ContentBottom(screenHeight);
            if(width<320 || height<130 || width>right-left ||
                height>bottom-top)
                return false;
            if(x<left) moveX=left-x;
            else if(x+width>right)moveX=right-(x+width);
            if(y<top)moveY=top-y;
            else if(y+height>bottom)moveY=bottom-(y+height);

            if(Math.Abs(moveX)>MaxContentShift ||
                Math.Abs(moveY)>112 ||
                x+moveX<left-0.5f ||
                x+moveX+width>right+0.5f ||
                y+moveY<top-0.5f ||
                y+moveY+height>bottom+0.5f)
            {
                moveX=0;
                moveY=0;
                return false;
            }
            return Math.Abs(moveX)>=1f || Math.Abs(moveY)>=1f;
        }

        /// <summary>
        /// A native Back/Continue button near the bottom edge gets the
        /// CU footer's 24px bottom spacing, but only with a bounded
        /// correction. No icon-only color swatch, scene or central button
        /// qualifies. Native click handlers stay on the original button.
        /// </summary>
        public static bool TryPlanFooter(string stage,
            int screenWidth,int screenHeight,
            float x,float y,float width,float height,
            out float moveX,out float moveY)
        {
            moveX=0;
            moveY=0;
            if(!CanAlignNativeFooter(stage) ||
                !IsViewportSupported(screenWidth,screenHeight) ||
                !Valid(x)||!Valid(y)||!Valid(width)||!Valid(height))
                return false;
            if(width<100 || width>430 || height<30 || height>75 ||
                y<screenHeight-160 || x<0 ||
                x+width>screenWidth+1)
                return false;
            float bottomGap=screenHeight-(y+height);
            if(bottomGap<0 || bottomGap>85)return false;
            bool left=x+width/2<screenWidth*0.30f;
            bool right=x+width/2>screenWidth*0.70f;
            if(!left && !right)return false;
            float desiredX=left?29:screenWidth-31-width;
            moveX=desiredX-x;
            moveY=bottomGap-24f;
            if(Math.Abs(moveX)>MaxFooterShift ||
                Math.Abs(moveY)>35f)
            {
                moveX=0;
                moveY=0;
                return false;
            }
            return Math.Abs(moveX)>=1f || Math.Abs(moveY)>=1f;
        }

        public static bool RootMatchesResolution(
            float rootWidth,float rootHeight,
            int screenWidth,int screenHeight)
        {
            if(!IsViewportSupported(screenWidth,screenHeight) ||
                !Valid(rootWidth) || !Valid(rootHeight))return false;
            // Abort rather than mix physical pixels and a scaled UI space.
            return Math.Abs(rootWidth-screenWidth)<=screenWidth*0.075f &&
                Math.Abs(rootHeight-screenHeight)<=screenHeight*0.075f;
        }

        private static bool Valid(float value)
        {
            return !Single.IsInfinity(value) &&
                !Single.IsNaN(value) && value>=0;
        }
    }
}
