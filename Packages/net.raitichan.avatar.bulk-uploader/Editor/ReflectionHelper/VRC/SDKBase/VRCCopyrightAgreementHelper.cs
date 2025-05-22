using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace net.raitichan.avatar.bulk_uploader.Editor.ReflectionHelper.VRC.SDKBase {
	internal static class VRCCopyrightAgreementHelper {
		private const string ASSEMBLY_NAME = "VRC.SDKBase.Editor";
		private const string NAMESPACE = "VRC.SDKBase";
		private const string CLASS_NAME = NAMESPACE + ".VRCCopyrightAgreement";
		private const string TYPE_NAME = CLASS_NAME + ", " + ASSEMBLY_NAME;


		private const string SAVE_CONTENT_AGREEMENT_TO_SESSION_METHOD_NAME = "SaveContentAgreementToSession";
		private static Action<string>? _saveContentAgreementToSession;

		internal static void SaveContentAgreementToSession(string contentId) {
			if (_saveContentAgreementToSession != null) {
				_saveContentAgreementToSession(contentId);
				return;
			}

			Type? type = Type.GetType(TYPE_NAME);
			if (type == null) throw new NullReferenceException($"Not found type {TYPE_NAME}");

			MethodInfo? methodInfo = type.GetMethod(SAVE_CONTENT_AGREEMENT_TO_SESSION_METHOD_NAME, BindingFlags.Static | BindingFlags.NonPublic);
			if (methodInfo == null) throw new NullReferenceException($"Not found method : {SAVE_CONTENT_AGREEMENT_TO_SESSION_METHOD_NAME}");

			_saveContentAgreementToSession = CreateStaticMethodCallAction<string>(methodInfo);
			_saveContentAgreementToSession(contentId);
		}

		private static Action<TArg0> CreateStaticMethodCallAction<TArg0>(MethodInfo methodInfo) {
			ParameterExpression arg0Parameter = Expression.Parameter(typeof(TArg0), "arg0");
			Expression<Action<TArg0>> expression = Expression.Lambda<Action<TArg0>>(
				Expression.Call(methodInfo, arg0Parameter),
				arg0Parameter);
			return expression.Compile();
		}
	}
}