using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using UnityEngine;

/// <summary>
/// Extension methods cho AnimationManager — thêm animation farming
/// mà không sửa file gốc của HeroEditor4D.
/// </summary>
public static class AnimationManagerExtension
{
    public static void Hoe(this AnimationManager anim)
    {
        anim.Animator.SetTrigger("Hoe");
        anim.IsAction = true;
    }

    public static void Scythe(this AnimationManager anim)
    {
        anim.Animator.SetTrigger("Scythe");
        anim.IsAction = true;
    }

    public static void Water(this AnimationManager anim)
    {
        anim.Animator.SetTrigger("Water");
        anim.IsAction = true;
    }

    public static void Sow(this AnimationManager anim)
    {
        anim.Animator.SetTrigger("Sow");
        anim.IsAction = true;
    }
}
