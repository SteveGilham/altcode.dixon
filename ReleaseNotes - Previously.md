# 2020.3.16.14542-pre-release

* `ReraiseCorrectlyRule` (`Dixon.Design#DX0002`) : only `throw` an exception you've just created

# 2020.3.16.14542-pre-release

* `ReraiseCorrectlyRule` (`Dixon.Design#DX0002`) : only `throw` and exception you've just created
# 2020.3.11.19262-pre-release

Proof of concept
* `JustifySuppressionRule` (`Dixon.Design#DX0001`), the "Hello, World!" of FxCop rules.  A port of the well travelled example rule to ensure that `SuppressMessage` attributes include a justification.  See e.g. http://www.binarycoder.net/fxcop/html/ex_specifysuppressmessagejustification.html