import React, { Component } from 'react';
import { connect } from 'react-redux';
import FacetedSearch from '../Components/FacetedSearch';
import { query, searchFacetChange } from '../Actions/FacetedSearch.action';

class FacetedSearchContainer extends Component {
    render() {
        return <FacetedSearch {...this.props} />;
    }
}
const mapStateToProps = ({facetedSearch:{facetFilters, navigationTheme}}) => {
    return {
        facetFilters,
        navigationTheme,
    }
}

const mapDispatchToProps = dispatch => {
    return {
        query: (q, replaceResult) => dispatch(query(q, replaceResult)),
        searchFacetChange: (group, item) => dispatch(searchFacetChange(group, item)),
    }
}

export default connect(mapStateToProps, mapDispatchToProps)(FacetedSearchContainer);