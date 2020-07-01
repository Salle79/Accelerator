(window.webpackJsonp=window.webpackJsonp||[]).push([[6],{641:function(e,a,t){"use strict";a.a=window.__litium.constants},938:function(e,a,t){"use strict";t.r(a);var n=t(53),r=t.n(n),l=t(2),o=t.n(l),s=t(27),m=t.n(s),c=t(28),i=t.n(c),u=t(29),p=t.n(u),d=t(30),f=t.n(d),v=t(31),b=t.n(v),h=t(0),_=t.n(h),N=t(12),E=t(671),g=t(3),y=t.n(g),j=t(15),O=function(e){function a(e){var t;return m()(this,a),(t=p()(this,f()(a).call(this,e))).state={removingRow:{}},t}return b()(a,e),i()(a,[{key:"onRemoveRequest",value:function(e,a){this.setState((function(t){return o()({},t,{removingRow:o()({},t.removingRow,r()({},e,a))})}))}},{key:"render",value:function(){var e=this,a=this.props,t=a.persons,n=a.onEdit,r=a.onRemove,l=this.state.removingRow;return _.a.createElement("div",{className:"simple-table"},_.a.createElement("div",{className:"row medium-unstack no-margin simple-table__header"},_.a.createElement("div",{className:"columns"},Object(j.a)("mypage.person.name")),_.a.createElement("div",{className:"columns"},Object(j.a)("mypage.person.email")),_.a.createElement("div",{className:"columns"},Object(j.a)("mypage.person.phone")),_.a.createElement("div",{className:"columns"},Object(j.a)("mypage.person.role")),_.a.createElement("div",{className:"columns medium-2 hide-for-small-only"})),t&&y()(t).call(t,(function(a){return _.a.createElement("div",{className:"row medium-unstack no-margin",key:a.systemId},_.a.createElement("div",{className:"columns"},a.firstName," ",a.lastName),_.a.createElement("div",{className:"columns"},a.email||""),_.a.createElement("div",{className:"columns"},a.phone||""),_.a.createElement("div",{className:"columns"},a.role),_.a.createElement("div",{className:"columns medium-2"},a.editable&&_.a.createElement(h.Fragment,null,_.a.createElement("a",{onClick:function(){return n(a)},className:"table__icon table__icon--edit",title:Object(j.a)("Edit")}),!l[a.systemId]&&_.a.createElement("a",{onClick:function(){return e.onRemoveRequest(a.systemId,!0)},className:"table__icon table__icon--delete",title:Object(j.a)("Remove")}),l[a.systemId]&&_.a.createElement("a",{className:"table__icon table__icon--accept",onClick:function(){return r(a.systemId)},title:Object(j.a)("Accept")}),l[a.systemId]&&_.a.createElement("a",{className:"table__icon table__icon--cancel",onClick:function(){return e.onRemoveRequest(a.systemId,!1)},title:Object(j.a)("Cancel")}))))})))}}]),a}(h.Component),w=t(641),C=function(e){var a=e.person,t=e.errors,n=void 0===t?{}:t,r=e.onDismiss,l=e.onChange,o=e.onSubmit;return _.a.createElement("div",null,_.a.createElement("h2",null,Object(j.a)(a.systemId?"mypage.person.edittitle":"mypage.person.addtitle")),_.a.createElement("div",{className:"row"},_.a.createElement("div",{className:"columns small-12 medium-8"},_.a.createElement("label",{className:"form__label",htmlFor:"firstName"},Object(j.a)("mypage.person.firstname")),_.a.createElement("input",{className:"form__input",id:"firstName",name:"firstName",type:"text",autoComplete:"given-name",value:a.firstName||"",onChange:function(e){return l("firstName",e.target.value)}}),n.firstName&&_.a.createElement("span",{className:"form__validator--error form__validator--top-narrow"},n.firstName[0])),_.a.createElement("div",{className:"columns small-12 medium-8"},_.a.createElement("label",{className:"form__label",htmlFor:"lastName"},Object(j.a)("mypage.person.lastname")),_.a.createElement("input",{className:"form__input",id:"lastName",name:"lastName",type:"text",autoComplete:"family-name",value:a.lastName||"",onChange:function(e){return l("lastName",e.target.value)}}),n.lastName&&_.a.createElement("span",{className:"form__validator--error form__validator--top-narrow"},n.lastName[0]))),_.a.createElement("div",{className:"row"},_.a.createElement("div",{className:"columns small-12 medium-8"},_.a.createElement("label",{className:"form__label",htmlFor:"email"},Object(j.a)("mypage.person.email")),_.a.createElement("input",{className:"form__input",id:"email",name:"email",type:"email",autoComplete:"email",value:a.email||"",onChange:function(e){return l("email",e.target.value)}}),n.email&&_.a.createElement("span",{className:"form__validator--error form__validator--top-narrow"},n.email[0])),_.a.createElement("div",{className:"columns small-12 medium-8"},_.a.createElement("label",{className:"form__label",htmlFor:"phone"},Object(j.a)("mypage.person.phone")),_.a.createElement("input",{className:"form__input",id:"phone",name:"phone",type:"tel",autoComplete:"tel",value:a.phone||"",onChange:function(e){return l("phone",e.target.value)}}),n.phone&&_.a.createElement("span",{className:"form__validator--error form__validator--top-narrow"},n.phone[0]))),_.a.createElement("div",{className:"row"},_.a.createElement("div",{className:"columns small-12 medium-8"},_.a.createElement("label",{className:"form__control"},_.a.createElement("input",{type:"radio",name:"role",className:"form__radio",value:w.a.role.approver,checked:a.role===w.a.role.approver,onChange:function(e){return l("role",e.target.value)}}),Object(j.a)("mypage.person.role.approver"))),_.a.createElement("div",{className:"columns small-12 medium-8"},_.a.createElement("label",{className:"form__control"},_.a.createElement("input",{type:"radio",name:"role",className:"form__radio",value:w.a.role.buyer,checked:a.role===w.a.role.buyer,onChange:function(e){return l("role",e.target.value)}}),Object(j.a)("mypage.person.role.buyer")))),n.general&&_.a.createElement("div",null,n.general[0]),_.a.createElement("button",{className:"form__button",onClick:function(){return r()}},Object(j.a)("general.cancel")),_.a.createElement("span",{className:"form__space"}),_.a.createElement("button",{className:"form__button",onClick:function(){return o(a)}},Object(j.a)("general.save")))},k=t(66),q=Object(E.object)().shape({phone:Object(E.string)().required(Object(j.a)("validation.required")),email:Object(E.string)().required(Object(j.a)("validation.required")).email(Object(j.a)("validation.email")),lastName:Object(E.string)().required(Object(j.a)("validation.required")),firstName:Object(E.string)().required(Object(j.a)("validation.required"))}),R=function(e){function a(e){var t;return m()(this,a),(t=p()(this,f()(a).call(this,e))).state={person:{}},t.props.query(),t}return b()(a,e),i()(a,[{key:"showForm",value:function(e){this.setState({person:e}),this.props.changeMode("edit")}},{key:"showList",value:function(){this.setState({person:{}}),this.props.changeMode("list")}},{key:"handlePersonInputChange",value:function(e,a){this.setState((function(t){return o()({},t,{person:o()({},t.person,r()({},e,a))})}))}},{key:"onPersonSubmit",value:function(e){var a=this;e&&e.editable&&q.validate(e).then((function(){e.systemId?a.props.edit(e):a.props.add(e)})).catch((function(e){return a.props.setError(e)}))}},{key:"render",value:function(){var e=this;return _.a.createElement(h.Fragment,null,"list"!==this.props.mode&&_.a.createElement(C,{person:this.state.person,errors:this.props.errors,onDismiss:function(){return e.showList()},onChange:function(a,t){return e.handlePersonInputChange(a,t)},onSubmit:function(a){return e.onPersonSubmit(a)}}),"list"===this.props.mode&&_.a.createElement(h.Fragment,null,_.a.createElement("h2",null,Object(j.a)("mypage.person.title")),_.a.createElement("p",null,_.a.createElement("b",null,Object(j.a)("mypage.person.subtitle"))),_.a.createElement("button",{className:"form__button",onClick:function(){return e.showForm({role:w.a.role.approver,editable:!0})}},Object(j.a)("mypage.person.add")),_.a.createElement(O,{persons:this.props.persons,onEdit:function(a){return e.showForm(a)},onRemove:function(a){return e.props.remove(a)}})))}}]),a}(h.Component);a.default=Object(N.b)((function(e){return{persons:e.myPage.persons.list,mode:e.myPage.persons.mode,errors:e.myPage.persons.errors}}),(function(e){return{query:function(){return e(Object(k.g)())},remove:function(a){return e(Object(k.h)(a))},add:function(a){return e(Object(k.d)(a))},edit:function(a){return e(Object(k.f)(a))},changeMode:function(a){return e(Object(k.e)(a))},setError:function(a){return e(Object(k.i)(a))}}}))(R)}}]);
//# sourceMappingURL=6.a700a962c07b2d16c5ba.js.map