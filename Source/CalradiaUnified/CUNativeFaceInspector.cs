using System;
using System.Reflection;
using SandBox.View.CharacterCreation;

namespace CalradiaUnified
{
    /// <summary>
    /// Observes, without constructing or modifying, the native v1.5.7
    /// CharacterCreationFaceGeneratorView and its BodyGeneratorView layers.
    ///
    /// This verifies that the active stage uses the real character, scene,
    /// camera and live slider UI. It does not relocate a scene viewport.
    /// </summary>
    internal static class CUNativeFaceInspector
    {
        public static bool TryInspect(CharacterCreationScreen screen,
            out string detail)
        {
            detail="FACE VIEW NOT ACTIVE";
            if(screen==null || screen.IsFinalized)
                return false;
            try
            {
                var currentViewField=typeof(CharacterCreationScreen).GetField(
                    "_currentStageView",
                    BindingFlags.Instance|BindingFlags.NonPublic);
                var currentView=currentViewField==null ? null :
                    currentViewField.GetValue(screen);
                if(currentView==null ||
                    currentView.GetType().Name!=
                        "CharacterCreationFaceGeneratorView")
                    return false;

                var bodyField=currentView.GetType().GetField(
                    "_faceGeneratorView",
                    BindingFlags.Instance|BindingFlags.NonPublic);
                var body=bodyField==null ? null :
                    bodyField.GetValue(currentView);
                if(body==null || body.GetType().Name!="BodyGeneratorView")
                {
                    detail="NATIVE BODY GENERATOR NOT FOUND";
                    return false;
                }
                var bodyType=body.GetType();
                var sceneProp=bodyType.GetProperty("SceneLayer");
                var controlsProp=bodyType.GetProperty("GauntletLayer");
                var generatorProp=bodyType.GetProperty("BodyGen");
                var scene=sceneProp==null ? null :
                    sceneProp.GetValue(body,null);
                var controls=controlsProp==null ? null :
                    controlsProp.GetValue(body,null);
                var generator=generatorProp==null ? null :
                    generatorProp.GetValue(body,null);
                if(scene==null || controls==null || generator==null)
                {
                    detail="NATIVE FACE SCENE OR CONTROLS MISSING";
                    return false;
                }
                bool realSceneOnScreen=false;
                bool realControlsOnScreen=false;
                foreach(var layer in screen.Layers)
                {
                    if(Object.ReferenceEquals(layer,scene))
                        realSceneOnScreen=true;
                    if(Object.ReferenceEquals(layer,controls))
                        realControlsOnScreen=true;
                }
                if (!realSceneOnScreen || !realControlsOnScreen)
                {
                    detail="NATIVE FACE LAYERS NOT BOTH ACTIVE";
                    return false;
                }
                detail="REAL 3D FACE SCENE + NATIVE SLIDERS ACTIVE";
                return true;
            }
            catch(Exception ex)
            {
                detail="FACE INSPECTION: "+ex.GetType().Name;
                return false;
            }
        }
    }
}
